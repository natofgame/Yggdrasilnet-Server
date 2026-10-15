using System.Runtime.CompilerServices;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

/// <summary>
///     Responsible for coordinating the AI group of a target entity.
/// </summary>
public sealed class AiGroupCoordinator {
    private readonly Dictionary<int, float> _gapTimers = new();

    private readonly Dictionary<int, List<Member>> _groups = new();
    private readonly List<Member> _orderedMembers = new();
    private readonly List<int> _staleTargetIds = new();
    public int MaxAttackersPerTarget { get; set; } = 3;
    public float TokenRange { get; set; } = 10f;
    public float TokenLease { get; set; } = 4f;
    public float MinGap { get; set; } = 0.25f;
    public float MaxGap { get; set; } = 1.8f;

    public float BaseSlotRadius { get; set; } = 2.4f;
    public float SlotRadiusPerAlly { get; set; } = 0.12f;
    public float SlotRadiusNoise { get; set; } = 1.4f;
    public float SlotAngleNoise { get; set; } = 0.9f;
    public float SlotRadiusWaveAmplitude { get; set; } = 0.75f;
    public float SlotRadiusWaveFrequency { get; set; } = 1.6f;
    public float SlotRadiusBlendSpeed { get; set; } = 3.2f;

    public float OrbitSwitchMinInterval { get; set; } = 0.8f;
    public float OrbitSwitchMaxInterval { get; set; } = 2.6f;
    public float OrbitSwitchProbability { get; set; } = 0.55f;

    public float SlotSwapMinInterval { get; set; } = 1.2f;
    public float SlotSwapMaxInterval { get; set; } = 3.5f;
    public float SlotSwapProbability { get; set; } = 0.4f;

    public float RepositionPulseMinInterval { get; set; } = 1.4f;
    public float RepositionPulseMaxInterval { get; set; } = 3.8f;
    public float RepositionPulseProbability { get; set; } = 0.5f;
    public float RepositionBurstMinDuration { get; set; } = 0.45f;
    public float RepositionBurstMaxDuration { get; set; } = 0.95f;
    public float RepositionSlotShiftInSteps { get; set; } = 0.85f;

    /// <summary>
    ///     Updates the AI group coordinator.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="dt"></param>
    public void Update(Dimension world, float dt) {
        foreach (var list in _groups.Values)
            list.Clear();

        foreach (var entity in world.Query<BrainComponent>().ToArray()) {
            var ai = entity.Get<BrainComponent>();
            if (ai.TargetEntityId is not { } targetId) {
                var changed = false;
                if (ai.HasAttackToken || ai.TokenTimer > 0f || ai.TokenWait > 0f) {
                    ReleaseToken(ref ai);
                    changed = true;
                }

                if (ai.RepositionPulseTimer > 0f || ai.RepositionBurstTimer > 0f) {
                    ai.RepositionPulseTimer = 0f;
                    ai.RepositionBurstTimer = 0f;
                    ai.SlotAngleOffset = 0f;
                    changed = true;
                }

                if (ai.AlliesNearby != 0) {
                    ai.AlliesNearby = 0;
                    changed = true;
                }

                if (changed) {
                    entity.Set(ai);
                }

                continue;
            }

            if (!_groups.TryGetValue(targetId, out var members)) {
                members = [];
                _groups[targetId] = members;
            }

            members.Add(new Member(entity.UniqueId, ai));
        }

        _staleTargetIds.Clear();
        foreach (var (targetId, members) in _groups) {
            if (members.Count == 0) {
                _staleTargetIds.Add(targetId);
                continue;
            }

            UpdateGroup(members, targetId, dt);
            PersistGroup(world, members);
        }

        foreach (var targetId in _staleTargetIds) {
            _groups.Remove(targetId);
            _gapTimers.Remove(targetId);
        }
    }

    /// <summary>
    ///     Updates the AI group of a target entity.
    /// </summary>
    /// <param name="members"></param>
    /// <param name="targetId"></param>
    /// <param name="dt"></param>
    private void UpdateGroup(List<Member> members, int targetId, float dt) {
        _gapTimers.TryGetValue(targetId, out var tokenGap);
        tokenGap = MathF.Max(0f, tokenGap - dt);

        AssignSlots(targetId, members, dt);

        var maxAttackers = Math.Clamp((members.Count + 1) / 2, 1, MaxAttackersPerTarget);
        var activeTokenCount = 0;
        for (var i = 0; i < members.Count; i++) {
            var member = members[i];
            var ai = member.Ai;
            ai.AlliesNearby = members.Count - 1;

            if (ai.HasAttackToken) {
                ai.TokenTimer = MathF.Max(0f, ai.TokenTimer - dt);
                if (ai.TokenTimer <= 0f
                    || ai.AttackCooldown > 0f
                    || ai.TargetDistance > TokenRange * 1.35f) {
                    ReleaseToken(ref ai);
                }
                else {
                    activeTokenCount++;
                }
            }
            else if (ai.AttackCooldown <= 0f && ai.TargetDistance <= TokenRange) {
                ai.TokenWait += dt;
            }
            else {
                ai.TokenWait = MathF.Max(0f, ai.TokenWait - dt * 0.5f);
            }

            member.Ai = ai;
            members[i] = member;
        }

        if (activeTokenCount < maxAttackers && tokenGap <= 0f) {
            var bestIndex = FindBestCandidate(members);
            if (bestIndex >= 0) {
                var member = members[bestIndex];
                var ai = member.Ai;
                ai.HasAttackToken = true;
                ai.TokenTimer = TokenLease;
                ai.TokenWait = 0f;
                member.Ai = ai;
                members[bestIndex] = member;

                tokenGap = MinGap + (float)Random.Shared.NextDouble() * MathF.Max(0f, MaxGap - MinGap);
            }
        }

        _gapTimers[targetId] = tokenGap;
    }

    /// <summary>
    ///     Find nearest member that can be attacked.
    /// </summary>
    /// <param name="members"></param>
    /// <returns></returns>
    private static int FindBestCandidate(List<Member> members) {
        var bestScore = float.MinValue;
        var bestIndex = -1;

        for (var i = 0; i < members.Count; i++) {
            var ai = members[i].Ai;
            if (ai.HasAttackToken || ai.AttackCooldown > 0f || ai.SurprisedTimer > 0f) {
                continue;
            }

            var score = ai.TokenWait
                        + ai.Aggressivity * 0.3f
                        + ai.Impulsivity * 0.1f
                        - ai.TargetDistance * 0.1f
                        + (1f - ai.HealthRatio) * 0.1f;
            if (!(score > bestScore)) {
                continue;
            }

            bestScore = score;
            bestIndex = i;
        }

        return bestIndex;
    }

    /// <summary>
    ///     Assigns slots to the members of the group.
    /// </summary>
    /// <param name="targetId"></param>
    /// <param name="members"></param>
    /// <param name="dt"></param>
    private void AssignSlots(int targetId, List<Member> members, float dt) {
        _orderedMembers.Clear();
        _orderedMembers.AddRange(members);
        _orderedMembers.Sort((a, b) => a.EntityId.CompareTo(b.EntityId));

        if (_orderedMembers.Count == 0) {
            return;
        }

        ShuffleSlots(_orderedMembers, dt);

        // Keep a stable anchor per target so the whole ring doesn't rotate
        // with a single member and force everyone to "follow in a train".
        var anchorAngle = Hash01((uint)targetId, 0u, 97) * MathF.Tau;
        var step = MathF.Tau / _orderedMembers.Count;
        var radius = BaseSlotRadius + MathF.Max(0f, _orderedMembers.Count - 1) * SlotRadiusPerAlly;
        var switchMin = MathF.Max(0.05f, OrbitSwitchMinInterval);
        var switchMax = MathF.Max(switchMin, OrbitSwitchMaxInterval);
        var switchProbability = Math.Clamp(OrbitSwitchProbability, 0f, 1f);
        var noiseAmplitude = MathF.Max(0f, SlotRadiusNoise);
        var waveAmplitude = MathF.Max(0f, SlotRadiusWaveAmplitude);
        var waveFrequency = MathF.Max(0f, SlotRadiusWaveFrequency);
        var blend = Math.Clamp(MathF.Max(0f, SlotRadiusBlendSpeed) * dt, 0f, 1f);
        var repositionPulseMin = MathF.Max(0.05f, RepositionPulseMinInterval);
        var repositionPulseMax = MathF.Max(repositionPulseMin, RepositionPulseMaxInterval);
        var repositionProbability = Math.Clamp(RepositionPulseProbability, 0f, 1f);
        var repositionBurstMin = MathF.Max(0.05f, RepositionBurstMinDuration);
        var repositionBurstMax = MathF.Max(repositionBurstMin, RepositionBurstMaxDuration);
        var repositionSlotShift = MathF.Max(0f, RepositionSlotShiftInSteps) * step;

        var updatedAi = new Dictionary<int, BrainComponent>(_orderedMembers.Count);
        for (var i = 0; i < _orderedMembers.Count; i++) {
            var member = _orderedMembers[i];
            var ai = member.Ai;

            var angleNoise = (Hash01((uint)member.EntityId, (uint)_orderedMembers.Count, 71) * 2f - 1f)
                             * MathF.Max(0f, SlotAngleNoise) * step;
            ai.SlotAngle = WrapAngle(anchorAngle + i * step + angleNoise + ai.SlotAngleOffset);
            var staticNoise = (Hash01((uint)member.EntityId, (uint)_orderedMembers.Count, 31) * 2f - 1f)
                              * noiseAmplitude;
            var phase = Hash01((uint)member.EntityId, (uint)member.Ai.TargetEntityId.GetValueOrDefault(), 53)
                        * MathF.Tau;
            var waveNoise = MathF.Sin(ai.CombatStateTimer * waveFrequency + phase) * waveAmplitude;
            var targetOffset = radius - BaseSlotRadius + staticNoise + waveNoise;
            ai.HoldRadiusOffset += (targetOffset - ai.HoldRadiusOffset) * blend;

            if (ai.HoldOrbitDirection is not 1f and not -1f) {
                // Avoid relying on raw entity id parity (some id allocators can
                // produce mostly same-parity ids), and seed direction per pair.
                ai.HoldOrbitDirection = Hash01((uint)member.EntityId, (uint)targetId, 13) < 0.5f
                    ? -1f
                    : 1f;
            }

            ai.HoldOrbitSwitchTimer = MathF.Max(0f, ai.HoldOrbitSwitchTimer - dt);
            if (ai.HoldOrbitSwitchTimer <= 0f) {
                if ((float)Random.Shared.NextDouble() < switchProbability) {
                    ai.HoldOrbitDirection = -ai.HoldOrbitDirection;
                }

                ai.HoldOrbitSwitchTimer =
                    switchMin + (float)Random.Shared.NextDouble() * (switchMax - switchMin);
            }

            ai.RepositionBurstTimer = MathF.Max(0f, ai.RepositionBurstTimer - dt);
            ai.RepositionPulseTimer = MathF.Max(0f, ai.RepositionPulseTimer - dt);
            if (ai.RepositionPulseTimer <= 0f) {
                if ((float)Random.Shared.NextDouble() < repositionProbability) {
                    var shift = repositionSlotShift;
                    // Slight randomization on commit amplitude to avoid synchronized jumps.
                    shift *= 0.7f + (float)Random.Shared.NextDouble() * 0.6f;
                    ai.SlotAngleOffset = WrapAngle(ai.SlotAngleOffset + ai.HoldOrbitDirection * shift);
                    ai.RepositionBurstTimer =
                        repositionBurstMin + (float)Random.Shared.NextDouble() *
                        (repositionBurstMax - repositionBurstMin);
                }

                ai.RepositionPulseTimer =
                    repositionPulseMin + (float)Random.Shared.NextDouble() * (repositionPulseMax - repositionPulseMin);
            }

            updatedAi[member.EntityId] = ai;
        }

        for (var i = 0; i < members.Count; i++) {
            var member = members[i];
            if (!updatedAi.TryGetValue(member.EntityId, out var ai)) {
                continue;
            }

            member.Ai = ai;
            members[i] = member;
        }
    }

    /// <summary>
    ///     Shuffles the order of the slots in the group.
    /// </summary>
    /// <param name="ordered"></param>
    /// <param name="dt"></param>
    private void ShuffleSlots(List<Member> ordered, float dt) {
        var count = ordered.Count;
        if (count < 2) {
            return;
        }

        var switchMin = MathF.Max(0.05f, SlotSwapMinInterval);
        var switchMax = MathF.Max(switchMin, SlotSwapMaxInterval);
        var probability = Math.Clamp(SlotSwapProbability, 0f, 1f);

        for (var i = 0; i < count; i++) {
            var ai = ordered[i].Ai;
            ai.SlotSwapTimer = MathF.Max(0f, ai.SlotSwapTimer - dt);
            ordered[i] = ordered[i] with { Ai = ai };
        }

        for (var i = 0; i < count; i++) {
            var ai = ordered[i].Ai;
            if (ai.SlotSwapTimer > 0f) {
                continue;
            }

            ai.SlotSwapTimer = switchMin + (float)Random.Shared.NextDouble() * (switchMax - switchMin);
            ordered[i] = ordered[i] with { Ai = ai };

            if ((float)Random.Shared.NextDouble() >= probability) {
                continue;
            }

            var goLeft = Random.Shared.Next(2) == 0;
            var neighborIndex = goLeft ? i - 1 : i + 1;
            if (neighborIndex < 0) {
                neighborIndex = i + 1;
            }
            else if (neighborIndex >= count) {
                neighborIndex = i - 1;
            }

            if (neighborIndex < 0 || neighborIndex >= count || neighborIndex == i) {
                continue;
            }

            var neighborAi = ordered[neighborIndex].Ai;
            neighborAi.SlotSwapTimer =
                switchMin + (float)Random.Shared.NextDouble() * (switchMax - switchMin);
            ordered[neighborIndex] = ordered[neighborIndex] with { Ai = neighborAi };

            (ordered[i], ordered[neighborIndex]) = (ordered[neighborIndex], ordered[i]);
        }
    }

    /// <summary>
    ///     Reassigns the token to the AI.
    /// </summary>
    /// <param name="ai"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReleaseToken(ref BrainComponent ai) {
        ai.HasAttackToken = false;
        ai.TokenTimer = 0f;
        ai.TokenWait = 0f;
    }

    /// <summary>
    ///     Wraps an angle to the range [0, 2pi]
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float WrapAngle(float angle) {
        angle %= MathF.Tau;
        if (angle < 0f) {
            angle += MathF.Tau;
        }

        return angle;
    }

    /// <summary>
    ///     Hash function
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="salt"></param>
    /// <returns></returns>
    private static float Hash01(uint a, uint b, uint salt) {
        var x = a;
        x ^= b * 0x9E3779B9u;
        x ^= salt * 0x85EBCA6Bu;
        x ^= x >> 16;
        x *= 0x7FEB352Du;
        x ^= x >> 15;
        x *= 0x846CA68Bu;
        x ^= x >> 16;
        return x / (float)uint.MaxValue;
    }

    /// <summary>
    ///     Persists the AI group to the dimension.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="members"></param>
    private static void PersistGroup(Dimension dimension, List<Member> members) {
        foreach (var member in members) {
            if (!dimension.TryGetEntity(member.EntityId, out var entity)) {
                continue;
            }

            entity.Set(member.Ai);
        }
    }

    /// <summary>
    ///     Member of an AI group.
    /// </summary>
    /// <param name="EntityId"></param>
    /// <param name="Ai"></param>
    private record struct Member(int EntityId, BrainComponent Ai);
}