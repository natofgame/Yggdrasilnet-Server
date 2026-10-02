using Yggdrasilnet.Server.Simulation.World.Component;

namespace Yggdrasilnet.Server.Simulation.Content.Ai;

public sealed class AiGroupCoordinator {
    public int MaxAttackersPerTarget { get; set; } = 2;
    public float TokenRange { get; set; } = 10f;
    public float TokenLease { get; set; } = 4f;
    public float MinGap { get; set; } = 0.4f;
    public float MaxGap { get; set; } = 1.2f;
    public float SlotAngleJitterFactor { get; set; } = 0.35f;
    public float SlotRadiusRingStep { get; set; } = 0.5f;
    public float SlotRadiusJitter { get; set; } = 0.45f;

    private readonly record struct Member(World.Entity Entity, AiComponent Ai);

    private readonly Dictionary<int, List<Member>> _groups = new();
    private readonly Dictionary<int, float> _gapTimers = new();
    private readonly List<int> _staleKeys = new();
    private readonly List<Member> _orderedMembers = new();

    public void Update(World.World world, float dt) {
        foreach (var list in _groups.Values) {
            list.Clear();
        }

        foreach (var (entity, ai) in world.Query<AiComponent>()) {
            if (ai.TargetEntityId is not { } targetId) {
                ReleaseToken(ai);
                ai.AlliesNearby = 0;
                continue;
            }

            if (!_groups.TryGetValue(targetId, out var list)) {
                list = new List<Member>();
                _groups[targetId] = list;
            }
            list.Add(new Member(entity, ai));
        }

        _staleKeys.Clear();
        foreach (var (targetId, members) in _groups) {
            if (members.Count == 0) {
                _staleKeys.Add(targetId);
                continue;
            }
            UpdateGroup(targetId, members, dt);
        }

        foreach (var key in _staleKeys) {
            _groups.Remove(key);
            _gapTimers.Remove(key);
        }
    }

    private void UpdateGroup(int targetId, List<Member> members, float dt) {
        _gapTimers.TryGetValue(targetId, out var gap);
        gap = MathF.Max(0f, gap - dt);

        AssignSlots(targetId, members);

        var maxAttackers = Math.Clamp((members.Count + 1) / 2, 1, MaxAttackersPerTarget);
        var holders = 0;

        foreach (var m in members) {
            var ai = m.Ai;
            ai.AlliesNearby = members.Count - 1;

            if (ai.HasAttackToken) {
                ai.TokenTimer -= dt;
                if (ai.AttackCooldown > 0f
                    || ai.TokenTimer <= 0f
                    || ai.TargetDistance > TokenRange * 1.5f) {
                    ReleaseToken(ai);
                } else {
                    holders++;
                }
            } else if (ai.AttackCooldown <= 0f) {
                ai.TokenWait += dt;
            }
        }

        if (holders < maxAttackers && gap <= 0f) {
            AiComponent? bestAi = null;
            var bestScore = float.MinValue;

            foreach (var m in members) {
                var ai = m.Ai;
                if (ai.HasAttackToken
                    || ai.AttackCooldown > 0f
                    || ai.SurprisedTimer > 0f
                    || ai.TargetDistance > TokenRange) {
                    continue;
                }

                var score = ai.TokenWait
                            + ai.Aggressivity * 0.3f
                            + ai.Impulsivity * 0.1f
                            - ai.TargetDistance * 0.1f;
                if (score > bestScore) {
                    bestScore = score;
                    bestAi = ai;
                }
            }

            if (bestAi is not null) {
                bestAi.HasAttackToken = true;
                bestAi.TokenTimer = TokenLease;
                bestAi.TokenWait = 0f;
                gap = MinGap + (float)Random.Shared.NextDouble() * (MaxGap - MinGap);
            }
        }

        _gapTimers[targetId] = gap;
    }

    private void AssignSlots(int targetId, List<Member> members) {
        _orderedMembers.Clear();
        _orderedMembers.AddRange(members);
        _orderedMembers.Sort((a, b) => a.Entity.Id.CompareTo(b.Entity.Id));

        var anchorAngle = AngleFromTarget(_orderedMembers[0].Ai);
        var step = MathF.Tau / _orderedMembers.Count;
        var jitterFactor = Math.Clamp(SlotAngleJitterFactor, 0f, 0.45f);
        var ringStep = MathF.Max(0f, SlotRadiusRingStep);
        var radiusJitter = MathF.Max(0f, SlotRadiusJitter);

        for (var i = 0; i < _orderedMembers.Count; i++) {
            var member = _orderedMembers[i];
            var entityId = member.Entity.Id;
            var angleNoise = Hash01((uint)entityId, (uint)targetId, 1);
            var radiusNoise = Hash01((uint)entityId, (uint)targetId, 2);

            var angleJitter = (angleNoise * 2f - 1f) * step * jitterFactor;
            member.Ai.SlotAngle = WrapAngle(anchorAngle + i * step + angleJitter);

            var ringOffset = ((i % 3) - 1) * ringStep;
            var randomRadiusOffset = (radiusNoise * 2f - 1f) * radiusJitter;
            member.Ai.HoldRadiusOffset = ringOffset + randomRadiusOffset;
            member.Ai.HoldOrbitDirection = ((entityId + targetId) & 1) == 0 ? 1f : -1f;
        }
    }

    private static float AngleFromTarget(AiComponent ai) =>
        MathF.Atan2(-ai.TargetDirection.Z, -ai.TargetDirection.X);

    private static void ReleaseToken(AiComponent ai) {
        ai.HasAttackToken = false;
        ai.TokenTimer = 0f;
        ai.TokenWait = 0f;
    }

    private static float Hash01(uint entityId, uint targetId, uint salt) {
        var x = entityId;
        x ^= targetId * 0x9E3779B9u;
        x ^= salt * 0x85EBCA6Bu;
        x ^= x >> 16;
        x *= 0x7FEB352Du;
        x ^= x >> 15;
        x *= 0x846CA68Bu;
        x ^= x >> 16;
        return x / (float)uint.MaxValue;
    }

    private static float WrapAngle(float angle) {
        angle %= MathF.Tau;
        if (angle < 0f) {
            angle += MathF.Tau;
        }
        return angle;
    }
}
