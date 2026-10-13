using Yggdrasilnet.Server.Core.Behavior.Models;
using Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Combat;

namespace Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;

public sealed class AttackBehavior : AiBehavior {
    public float Speed { get; set; } = 7f;
    public float CastRange { get; set; } = 1.2f;
    public float CircleWeight { get; set; } = 0.3f;
    public float ArrivalBand { get; set; } = 2f;
    public float MinApproachFactor { get; set; } = 0.35f;
    public float CircleStartDistance { get; set; } = 1.6f;
    public float CircleNearRangeFactor { get; set; } = 0.15f;
    public int SpellIndex { get; set; }
    public float MinCooldown { get; set; } = 0.9f;
    public float MaxCooldown { get; set; } = 2.4f;
    public float CooldownJitter { get; set; } = 0.4f;
    public bool RandomReadySpell { get; set; } = true;

    public override AiCombatState State => AiCombatState.Pressure;
    public override float TransitionLockDuration => 0.25f;

    public override float Evaluate(in AiContext context, in BrainComponent ai) {
        if (!context.HasTarget || !context.HasAttackToken || context.AttackCooldown > 0f) {
            return 0f;
        }

        var distancePressure = 1f - Math.Clamp(
            (context.TargetDistance - CastRange) / MathF.Max(0.1f, CastRange * 2.5f),
            0f,
            1f);
        var score = 0.35f
                    + context.Aggressivity01 * 0.35f
                    + (1f - context.TargetHealthRatio) * 0.2f
                    + distancePressure * 0.3f;

        if (context.TargetDistance <= CastRange * 1.25f) {
            score += 0.2f;
        }

        return score;
    }

    public override AiIntent BuildIntent(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt
    ) {
        if (!context.HasTarget) {
            return AiIntent.Stop();
        }

        if (TrySelectCastSlot(dimension, entity, context.TargetDistance, out var slotIndex)) {
            ai.AttackCooldown = ResolveCooldown(ai.Aggressivity);
            return AiIntent.Cast(slotIndex);
        }

        if (!context.HasDirection) {
            return AiIntent.Stop();
        }

        var approachRange = ResolveApproachRange(dimension, entity);
        var overRange = MathF.Max(0f, context.TargetDistance - approachRange);
        var speedFactor = Math.Clamp(overRange / MathF.Max(0.05f, ArrivalBand), MinApproachFactor, 1f);
        var orbitDirection = ai.HoldOrbitDirection is 1f or -1f ? ai.HoldOrbitDirection : 1f;
        var circleWeight = context.TargetDistance > CircleStartDistance
            ? 0f
            : CircleWeight * CircleNearRangeFactor;

        return AiIntent.Move(
            context.TargetDirection,
            Speed * speedFactor,
            0f,
            approachRange,
            orbitDirection,
            circleWeight);
    }

    private bool TrySelectCastSlot(
        Dimension dimension,
        Entity entity,
        float targetDistance,
        out int slotIndex
    ) {
        slotIndex = -1;

        if (!entity.TryGet<CombatComponent>(out var combat)
            || combat.Slots.Length == 0) {
            return false;
        }

        var candidateCount = 0;
        for (var i = 0; i < combat.Slots.Length; i++) {
            var slot = combat.Slots[i];
            if (!TryGetReadySpellDefinition(slot, dimension, out var spellDef)) {
                continue;
            }

            var slotRange = ResolveCastRange(spellDef);
            if (targetDistance > slotRange) {
                continue;
            }

            if (!RandomReadySpell) {
                slotIndex = i;
                return true;
            }

            candidateCount++;
            if (Random.Shared.Next(candidateCount) == 0) {
                slotIndex = i;
            }
        }

        return slotIndex >= 0;
    }

    private float ResolveApproachRange(Dimension dimension, Entity entity) {
        if (!entity.TryGet<CombatComponent>(out var combat)
            || combat.Slots.Length == 0) {
            return ResolveCastRange(dimension, entity);
        }

        var best = 0f;
        for (var i = 0; i < combat.Slots.Length; i++) {
            if (!TryGetReadySpellDefinition(combat.Slots[i], dimension, out var spellDef)) {
                continue;
            }

            var slotRange = ResolveCastRange(spellDef);
            if (slotRange > best) {
                best = slotRange;
            }
        }

        return best > 0f ? best : ResolveCastRange(dimension, entity);
    }

    private static bool TryGetReadySpellDefinition(
        CombatSlot slot,
        Dimension dimension,
        out BehaviorDefinition spellDef
    ) {
        spellDef = null!;
        if (string.IsNullOrEmpty(slot.SpellId) || slot.CooldownRemaining > 0f) {
            return false;
        }

        if (!dimension.TryGetSpell(slot.SpellId, out spellDef)
            || !spellDef.TryGetBehavior<SpellTimingBehavior>(out _)) {
            return false;
        }

        if (!spellDef.TryGetBehavior<SpellChargesBehavior>(out var charges)
            || charges.ResolvedType != SpellChargePolicyType.Regenerating
            || charges.MaxCharges <= 0) {
            return true;
        }

        var chargesCurrent = slot.Initialized
            ? slot.ChargesCurrent
            : charges.StartFull
                ? charges.MaxCharges
                : 0;

        return chargesCurrent > 0;
    }

    private float ResolveCastRange(Dimension dimension, Entity entity) {
        if (!entity.TryGet<CombatComponent>(out var combat)
            || SpellIndex < 0
            || SpellIndex >= combat.Slots.Length) {
            return CastRange;
        }

        var spellId = combat.Slots[SpellIndex].SpellId;
        if (string.IsNullOrEmpty(spellId) || !dimension.TryGetSpell(spellId, out var spellDef)) {
            return CastRange;
        }

        return ResolveCastRange(spellDef);
    }

    private float ResolveCastRange(BehaviorDefinition spellDef) {
        if (spellDef.TryGetBehavior<SpellTargetingBehavior>(out var targeting) && targeting.Range > 0f) {
            return targeting.Range;
        }

        if (!spellDef.TryGetBehavior<SpellProjectileBehavior>(out var projectile)) {
            return CastRange;
        }

        return projectile.AutoTargetRange > 0f
            ? projectile.AutoTargetRange
            : MathF.Max(0.05f, projectile.Speed * projectile.Lifetime);
    }

    private float ResolveCooldown(float aggressivity) {
        var t = Math.Clamp(aggressivity / 10f, 0f, 1f);
        var baseCooldown = MathF.Max(0.05f, MaxCooldown + (MinCooldown - MaxCooldown) * t);
        var jitter = ((float)Random.Shared.NextDouble() * 2f - 1f) * CooldownJitter;
        return MathF.Max(0.05f, baseCooldown + jitter);
    }
}
