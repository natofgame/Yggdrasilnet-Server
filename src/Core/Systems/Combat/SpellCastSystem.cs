using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Behavior.Models;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Events;
using Yggdrasilnet.Server.Core.Events.Events;
using Yggdrasilnet.Server.Core.World.Content.Spell;
using Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;
using Yggdrasilnet.Server.Core.World.Content.Spell.Effect.Handlers;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Combat;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;
using CollisionLayer = Yggdrasilnet.NetworkV4.Enums.CollisionLayer;

namespace Yggdrasilnet.Server.Core.Systems.Combat;

/// <summary>
///  Handles casting of spells.
/// </summary>
/// <param name="behaviors"></param>
public sealed class SpellCastSystem(BehaviorPackRegistry behaviors) : ISystem {
    public const string Identifier = "spell_cast";
    private const float MinDirectionSq = 0.0001f;
    private const float InterceptEpsilon = 0.0001f;

    private static readonly Dictionary<SpellEffectType, ISpellEffectHandler> EffectHandlers = new() {
        [SpellEffectType.Damage] = new DamageEffectHandler(),
        [SpellEffectType.Heal] = new HealEffectHandler(),
        [SpellEffectType.Dash] = new DashEffectHandler(),
        [SpellEffectType.Punch] = new PunchEffectHandler()
    };

    private readonly BehaviorPackRegistry _behaviors = behaviors ?? throw new ArgumentNullException(nameof(behaviors));
    private readonly Queue<(int EntityId, int SlotIndex)> _pendingCasts = new();

    /// <inheritdoc/>
    public string Id => Identifier;
    
    /// <inheritdoc/>
    public SystemPhase Phase => SystemPhase.Combat;

    /// <inheritdoc/>
    public void Update(Dimension dimension, float dt) {
        DrainPendingCasts(dimension);

        if (!float.IsFinite(dt) || dt <= 0f) {
            return;
        }

        foreach (var entity in dimension.Query<CombatComponent>().ToArray()) {
            var combat = entity.Get<CombatComponent>();
            var next = combat;
            TickSlots(ref next, dt);

            if (next.IsCasting) {
                TickActiveCast(dimension, entity, ref next, dt);
            }

            entity.Set(next);
        }
    }

    /// <summary>
    ///  Handles the request to cast a spell.
    /// </summary>
    /// <param name="e"></param>
    [SubscribeWorldEvent]
    private void OnSpellCastRequest(SpellCastRequestEvent e) {
        _pendingCasts.Enqueue((e.EntityId, e.SlotIndex));
    }

    /// <summary>
    ///  Handles the request to cancel a spell.
    /// </summary>
    /// <param name="dimension"></param>
    private void DrainPendingCasts(Dimension dimension) {
        while (_pendingCasts.Count > 0) {
            var (entityId, slotIndex) = _pendingCasts.Dequeue();
            TryBeginCast(dimension, entityId, slotIndex);
        }
    }

    /// <summary>
    ///  Tries to begin casting a spell.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="entityId"></param>
    /// <param name="slotIndex"></param>
    private void TryBeginCast(Dimension dimension, int entityId, int slotIndex) {
        if (!dimension.TryGetEntity(entityId, out var entity)) {
            return;
        }

        if (!entity.TryGet<CombatComponent>(out var combat) || combat.IsCasting) {
            return;
        }

        if (slotIndex < 0 || slotIndex >= combat.Slots.Length) {
            return;
        }

        var slot = combat.Slots[slotIndex];
        if (string.IsNullOrEmpty(slot.SpellId)) {
            return;
        }

        if (!entity.TryGet<LivingComponent>(out var living) || !living.IsAlive) {
            return;
        }

        if (!_behaviors.TryGetSpell(slot.SpellId, out var spellDef) ||
            !spellDef.TryGetBehavior<SpellTimingBehavior>(out var timing)) {
            return;
        }

        spellDef.TryGetBehavior<SpellChargesBehavior>(out var charges);
        EnsureInitialized(charges, ref slot);

        if (slot.CooldownRemaining > 0f) {
            return;
        }

        if (charges is { ResolvedType: SpellChargePolicyType.Regenerating, MaxCharges: > 0 } &&
            slot.ChargesCurrent <= 0) {
            return;
        }

        if (!dimension.TryGetEntity(entityId, out var casterView)) {
            return;
        }

        ConsumeCharges(charges, ref slot);
        StartCooldown(spellDef, ref slot);
        ApplyCastStacks(spellDef, ref slot);
        combat.Slots[slotIndex] = slot;

        combat.IsCasting = true;
        combat.CastSlot = slotIndex;
        combat.CastSpellId = slot.SpellId;
        combat.CastPhase = SpellPhase.Anticipation;
        combat.PhaseDuration = timing.Anticipation;
        combat.PhaseTimer = timing.Anticipation;
        combat.CastOrigin = casterView.Position;
        combat.CastDirection = ResolveDirection(dimension, entity, casterView.Position, spellDef);
        combat.CastSpellType = spellDef.TryGetBehavior<SpellMetaBehavior>(out var meta)
            ? meta.ResolvedType
            : SpellType.Self;
        combat.CastHasDashEffect = spellDef.TryGetBehavior<SpellEffectsBehavior>(out var dashEffects)
                                   && dashEffects.Effects.Any(effect => effect.ResolvedType == SpellEffectType.Dash);

        entity.Set(combat);
    }

    /// <summary>
    ///  Tries to cast a spell.
    /// </summary>
    /// <param name="charges"></param>
    /// <param name="slot"></param>
    private static void EnsureInitialized(SpellChargesBehavior? charges, ref CombatSlot slot) {
        if (slot.Initialized) {
            return;
        }

        slot.Initialized = true;
        if (charges is not null && charges.ResolvedType == SpellChargePolicyType.Regenerating &&
            charges.MaxCharges > 0) {
            slot.ChargesCurrent = charges.StartFull ? charges.MaxCharges : 0;
        }
    }

    /// <summary>
    ///  Consumes a charge from a spell.
    /// </summary>
    /// <param name="charges"></param>
    /// <param name="slot"></param>
    private static void ConsumeCharges(SpellChargesBehavior? charges, ref CombatSlot slot) {
        if (charges is null || charges.ResolvedType != SpellChargePolicyType.Regenerating || charges.MaxCharges <= 0) {
            return;
        }

        slot.ChargesCurrent = Math.Max(0, slot.ChargesCurrent - 1);
        if (slot.ChargesCurrent < charges.MaxCharges && slot.ChargeRecoveryRemaining <= 0f) {
            slot.ChargeRecoveryRemaining = charges.RecoverySeconds;
        }
    }

    /// <summary>
    ///  Starts a cooldown for a spell.
    /// </summary>
    /// <param name="spellDef"></param>
    /// <param name="slot"></param>
    private static void StartCooldown(BehaviorDefinition spellDef, ref CombatSlot slot) {
        if (!spellDef.TryGetBehavior<SpellCooldownBehavior>(out var cooldown)
            || cooldown.ResolvedType != SpellCooldownPolicyType.Fixed
            || cooldown.DurationSeconds <= 0f) {
            return;
        }

        slot.CooldownRemaining = cooldown.DurationSeconds;
    }

    /// <summary>
    ///  Applies stacks to a spell.
    /// </summary>
    /// <param name="spellDef"></param>
    /// <param name="slot"></param>
    private static void ApplyCastStacks(BehaviorDefinition spellDef, ref CombatSlot slot) {
        if (!spellDef.TryGetBehavior<SpellStacksBehavior>(out var stacks)
            || stacks.ResolvedType != SpellStackPolicyType.Additive
            || stacks.MaxStacks <= 0) {
            return;
        }

        slot.Stacks = Math.Min(stacks.MaxStacks, slot.Stacks + 1);
        if (stacks.DurationSeconds > 0f && (stacks.RefreshDurationOnAdd || slot.StackRemaining <= 0f)) {
            slot.StackRemaining = stacks.DurationSeconds;
        }
    }

    /// <summary>
    ///  Tick the spell casting system.
    /// </summary>
    /// <param name="combat"></param>
    /// <param name="dt"></param>
    private void TickSlots(ref CombatComponent combat, float dt) {
        for (var i = 0; i < combat.Slots.Length; i++) {
            var slot = combat.Slots[i];
            if (string.IsNullOrEmpty(slot.SpellId) || !_behaviors.TryGetSpell(slot.SpellId, out var spellDef)) {
                continue;
            }

            if (slot.CooldownRemaining > 0f) {
                slot.CooldownRemaining = MathF.Max(0f, slot.CooldownRemaining - dt);
            }

            if (spellDef.TryGetBehavior<SpellChargesBehavior>(out var charges)
                && charges.ResolvedType == SpellChargePolicyType.Regenerating
                && charges.MaxCharges > 0) {
                TickCharges(charges, ref slot, dt);
            }

            if (slot.Stacks > 0 && spellDef.TryGetBehavior<SpellStacksBehavior>(out var stacks)
                                && stacks.DurationSeconds > 0f) {
                slot.StackRemaining = MathF.Max(0f, slot.StackRemaining - dt);
                if (slot.StackRemaining <= 0f) {
                    slot.Stacks = 0;
                }
            }

            combat.Slots[i] = slot;
        }
    }

    /// <summary>
    ///  Tick the spell casting system.
    /// </summary>
    /// <param name="charges"></param>
    /// <param name="slot"></param>
    /// <param name="dt"></param>
    private static void TickCharges(SpellChargesBehavior charges, ref CombatSlot slot, float dt) {
        if (slot.ChargesCurrent >= charges.MaxCharges || charges.RecoverySeconds <= 0f) {
            slot.ChargeRecoveryRemaining = 0f;
            return;
        }

        slot.ChargeRecoveryRemaining -= dt;
        while (slot.ChargeRecoveryRemaining <= 0f && slot.ChargesCurrent < charges.MaxCharges) {
            slot.ChargesCurrent++;
            if (slot.ChargesCurrent >= charges.MaxCharges) {
                slot.ChargeRecoveryRemaining = 0f;
                break;
            }

            slot.ChargeRecoveryRemaining += charges.RecoverySeconds;
        }
    }

    /// <summary>
    ///  Tick the spell casting system.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="combat"></param>
    /// <param name="dt"></param>
    /// <exception cref="Exception"></exception>
    private void TickActiveCast(Dimension dimension, Entity caster, ref CombatComponent combat, float dt) {
        if (!_behaviors.TryGetSpell(combat.CastSpellId, out var spellDef)
            || !spellDef.TryGetBehavior<SpellTimingBehavior>(out var timing)
            || !caster.TryGet<LivingComponent>(out var living)
            || !living.IsAlive) {
            ResetCast(ref combat);
            return;
        }

        var remaining = dt;
        while (combat.CastPhase != SpellPhase.None) {
            if (combat.PhaseTimer > remaining) {
                combat.PhaseTimer -= remaining;
                break;
            }

            remaining -= MathF.Max(0f, combat.PhaseTimer);

            if (combat.CastPhase is SpellPhase.Anticipation or SpellPhase.Strike) {
                ApplyPhase(dimension, caster, combat, spellDef, combat.CastPhase);
            }

            var skipReturn = spellDef.TryGetBehavior<SpellMetaBehavior>(out var meta) && meta.SkipReturnPhase;
            var nextPhase = combat.CastPhase switch {
                SpellPhase.Anticipation => SpellPhase.Strike,
                SpellPhase.Strike => SpellPhase.Impact,
                SpellPhase.Impact => skipReturn ? SpellPhase.None : SpellPhase.Return,
                _ => SpellPhase.None
            };

            combat.CastPhase = nextPhase;
            if (nextPhase == SpellPhase.None) {
                ResetCast(ref combat);
                break;
            }

            combat.PhaseDuration = nextPhase switch {
                SpellPhase.Strike => timing.Strike,
                SpellPhase.Impact => timing.Impact,
                SpellPhase.Return => timing.Return,
                _ => throw new Exception("Unknown SpellPhase: " + nextPhase)
            };
            combat.PhaseTimer = combat.PhaseDuration;
        }
    }

    /// <summary>
    ///  Resets the spell casting system.
    /// </summary>
    /// <param name="combat"></param>
    private static void ResetCast(ref CombatComponent combat) {
        combat.IsCasting = false;
        combat.CastSlot = -1;
        combat.CastSpellId = string.Empty;
        combat.CastPhase = SpellPhase.None;
        combat.PhaseTimer = 0f;
        combat.PhaseDuration = 0f;
    }

    /// <summary>
    ///  Applies a phase to a spell.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="combat"></param>
    /// <param name="spellDef"></param>
    /// <param name="phase"></param>
    private void ApplyPhase(
        Dimension dimension,
        Entity caster,
        CombatComponent combat,
        BehaviorDefinition spellDef,
        SpellPhase phase
    ) {
        var spellType = spellDef.TryGetBehavior<SpellMetaBehavior>(out var meta) ? meta.ResolvedType : SpellType.Self;
        var context = new SpellCastContext(caster.UniqueId, combat.CastSpellId, spellType, combat.CastDirection,
            combat.CastOrigin);

        var hasEffects = spellDef.TryGetBehavior<SpellEffectsBehavior>(out var effects);
        if (hasEffects) {
            ApplySelfEffects(dimension, context, caster, effects, phase);
        }

        if (phase != SpellPhase.Strike) {
            return;
        }

        if (spellDef.TryGetBehavior<SpellProjectileBehavior>(out var projectile)) {
            SpawnProjectile(dimension, caster, context, spellDef, projectile);
            return;
        }

        if (hasEffects && spellDef.TryGetBehavior<SpellTargetingBehavior>(out var targeting)) {
            ApplyTargetEffects(dimension, context, caster, targeting, effects, phase);
        }
    }

    /// <summary>
    ///  Applies effects to the caster.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="context"></param>
    /// <param name="caster"></param>
    /// <param name="effects"></param>
    /// <param name="phase"></param>
    private static void ApplySelfEffects(
        Dimension dimension,
        SpellCastContext context,
        Entity caster,
        SpellEffectsBehavior effects,
        SpellPhase phase
    ) {
        foreach (var effect in effects.Effects) {
            if (effect.ResolvedTarget != SpellEffectTarget.Self || !IsActive(effect, phase)) {
                continue;
            }

            EffectHandlers[effect.ResolvedType].Apply(dimension, context, caster, effect.Amount, effect.Duration);
        }
    }

    /// <summary>
    ///  Applies effects to the target.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="context"></param>
    /// <param name="caster"></param>
    /// <param name="targeting"></param>
    /// <param name="effects"></param>
    /// <param name="phase"></param>
    private static void ApplyTargetEffects(
        Dimension dimension,
        SpellCastContext context,
        Entity caster,
        SpellTargetingBehavior targeting,
        SpellEffectsBehavior effects,
        SpellPhase phase
    ) {
        var enemyLayer = ResolveEnemyLayer(caster);
        var rangeSq = targeting.Range * targeting.Range;

        foreach (var target in dimension.Query<CollisionComponent>()) {
            var collider = target.Get<CollisionComponent>();
            if (target.UniqueId == caster.UniqueId || collider.Layer != enemyLayer) {
                continue;
            }

            if (!caster.TryGet<LivingComponent>(out var living) || !living.IsAlive) {
                continue;
            }

            if ((target.Position - context.Origin).LengthSquared() > rangeSq) {
                continue;
            }

            foreach (var effect in effects.Effects) {
                if (effect.ResolvedTarget != SpellEffectTarget.Target || !IsActive(effect, phase)) {
                    continue;
                }

                EffectHandlers[effect.ResolvedType].Apply(dimension, context, target, effect.Amount, effect.Duration);
            }
        }
    }

    /// <summary>
    ///  Applies a stack to a spell.
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="phase"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsActive(SpellEffectEntry effect, SpellPhase phase) {
        return phase switch {
            SpellPhase.Anticipation => effect.ActiveOnAnticipation,
            SpellPhase.Strike => !effect.ActiveOnAnticipation,
            _ => false
        };
    }

    /// <summary>
    ///  Applies a stack to a spell.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="context"></param>
    /// <param name="spellDef"></param>
    /// <param name="projectile"></param>
    private static void SpawnProjectile(
        Dimension dimension,
        Entity caster,
        SpellCastContext context,
        BehaviorDefinition spellDef,
        SpellProjectileBehavior projectile
    ) {
        var spawnPosition = caster.Position
                            + context.Direction * projectile.SpawnForward
                            + Vector3.UnitY * projectile.SpawnHeight;
        var direction = ResolvePlanarDirection(context.Direction);

        var spawned = dimension.Spawn(projectile.EntityDefinitionId, spawnPosition);
        var targetLayer = ResolveEnemyLayer(caster);

        spawned.Set(new ProjectileMotionComponent {
            Velocity = direction * projectile.Speed,
            Direction = direction,
            Speed = projectile.Speed,
            AutoTargetEnabled = projectile.AutoTargetEnabled,
            AutoTargetInfluence = projectile.AutoTargetInfluence,
            AutoTargetRange = projectile.AutoTargetRange,
            RemainingLifetimeSeconds = projectile.Lifetime
        });

        spawned.Set(new ProjectileImpactComponent {
            OwnerEntityId = caster.UniqueId,
            TargetLayer = targetLayer,
            Damage = ResolveProjectileDamage(spellDef),
            AreaDamageEnabled = projectile.AreaDamageEnabled,
            AreaDamageRadius = projectile.AreaDamageRadius,
            RemainingHits = projectile.MaxHits,
            HitEntityIds = null,
            SpellId = spellDef.Id
        });
    }

    /// <summary>
    ///  Resolves the direction of a spell.
    /// </summary>
    /// <param name="spellDef"></param>
    /// <returns></returns>
    private static float ResolveProjectileDamage(BehaviorDefinition spellDef) {
        if (!spellDef.TryGetBehavior<SpellEffectsBehavior>(out var effects)) {
            return 0f;
        }

        foreach (var effect in effects.Effects)
            if (effect.ResolvedTarget == SpellEffectTarget.Target && effect.ResolvedType == SpellEffectType.Damage) {
                return effect.Amount;
            }

        return 0f;
    }

    /// <summary>
    ///  Resolves the direction of a spell.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="casterPosition"></param>
    /// <param name="spellDef"></param>
    /// <returns></returns>
    private static Vector3 ResolveDirection(
        Dimension dimension,
        Entity caster,
        Vector3 casterPosition,
        BehaviorDefinition spellDef
    ) {
        var enemyLayer = ResolveEnemyLayer(caster);
        if (spellDef.TryGetBehavior<SpellProjectileBehavior>(out var projectile)
            && TryResolveProjectileLeadDirection(dimension, caster, casterPosition, projectile, enemyLayer,
                out var leadDirection)) {
            return leadDirection;
        }

        if (TryResolveTargetDirection(dimension, caster, casterPosition, enemyLayer, out var targetDirection)) {
            return targetDirection;
        }

        var aimRadius = ResolveAimRadius(spellDef);
        if (TryFindNearestEnemy(dimension, caster.UniqueId, casterPosition, enemyLayer, aimRadius,
                out var nearestPosition)) {
            var toTarget = (nearestPosition - casterPosition) with { Y = 0f };
            if (toTarget.LengthSquared() > MinDirectionSq) {
                return Vector3.Normalize(toTarget);
            }
        }

        if (!caster.TryGet<MovementComponent>(out var movement)) {
            return Vector3.UnitZ;
        }

        var flat = movement.Velocity with { Y = 0f };
        return flat.LengthSquared() > MinDirectionSq ? Vector3.Normalize(flat) : Vector3.UnitZ;
    }

    /// <summary>
    ///   Determines if the spell can be cast.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="casterPosition"></param>
    /// <param name="projectile"></param>
    /// <param name="enemyLayer"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static bool TryResolveProjectileLeadDirection(
        Dimension dimension,
        Entity caster,
        Vector3 casterPosition,
        SpellProjectileBehavior projectile,
        CollisionLayer enemyLayer,
        out Vector3 direction
    ) {
        direction = default;
        if (projectile.Speed <= 0f) {
            return false;
        }

        if (!TryResolveLockedTargetEntity(dimension, caster, enemyLayer, out var target)) {
            return false;
        }

        var toTarget = (target.Position - casterPosition) with { Y = 0f };
        if (toTarget.LengthSquared() <= MinDirectionSq) {
            return false;
        }

        var casterVelocity = ResolvePlanarVelocity(caster);
        var targetVelocity = ResolvePlanarVelocity(target);
        var relativeVelocity = targetVelocity - casterVelocity;

        if (!TrySolveInterceptTime(toTarget, relativeVelocity, projectile.Speed, out var interceptTime)) {
            direction = Vector3.Normalize(toTarget);
            return true;
        }

        var aimOffset = toTarget + relativeVelocity * interceptTime;
        if (aimOffset.LengthSquared() <= MinDirectionSq) {
            direction = Vector3.Normalize(toTarget);
            return true;
        }

        direction = Vector3.Normalize(aimOffset);
        return true;
    }

    /// <summary>
    ///  Resolves the direction of a spell.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="casterPosition"></param>
    /// <param name="enemyLayer"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static bool TryResolveTargetDirection(
        Dimension dimension,
        Entity caster,
        Vector3 casterPosition,
        CollisionLayer enemyLayer,
        out Vector3 direction
    ) {
        direction = default;
        if (!caster.TryGet<TargetComponent>(out var target)) {
            return false;
        }

        if (target.EntityId.HasValue
            && TryResolveLockedTargetEntity(dimension, caster, enemyLayer, out var targetEntity)) {
            var toTarget = (targetEntity.Position - casterPosition) with { Y = 0f };
            if (toTarget.LengthSquared() > MinDirectionSq) {
                direction = Vector3.Normalize(toTarget);
                return true;
            }
        }

        var storedDirection = target.Direction with { Y = 0f };
        if (storedDirection.LengthSquared() <= MinDirectionSq) {
            return false;
        }

        direction = Vector3.Normalize(storedDirection);
        return true;
    }

    /// <summary>
    ///  Resolves the direction of a spell.
    /// </summary>
    /// <param name="spellDef"></param>
    /// <returns></returns>
    private static float ResolveAimRadius(BehaviorDefinition spellDef) {
        if (!spellDef.TryGetBehavior<SpellTargetingBehavior>(out var targeting)) {
            return float.MaxValue;
        }

        var min = targeting.HitBoxMin;
        var max = targeting.HitBoxMax;
        var extentX = MathF.Max(MathF.Abs(min.X), MathF.Abs(max.X));
        var extentZ = MathF.Max(MathF.Abs(min.Z), MathF.Abs(max.Z));

        if (targeting.CardinalHitBoxMin is not { } cardMin || targeting.CardinalHitBoxMax is not { } cardMax) {
            return MathF.Max(0f, targeting.Range) + MathF.Max(extentX, extentZ);
        }

        extentX = MathF.Max(extentX, MathF.Max(MathF.Abs(cardMin.X), MathF.Abs(cardMax.X)));
        extentZ = MathF.Max(extentZ, MathF.Max(MathF.Abs(cardMin.Z), MathF.Abs(cardMax.Z)));

        return MathF.Max(0f, targeting.Range) + MathF.Max(extentX, extentZ);
    }

    /// <summary>
    ///  Resole Enemy Layer
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CollisionLayer ResolveEnemyLayer(Entity entity) {
        if (entity.TryGet<CollisionComponent>(out var collider) && collider.Layer == CollisionLayer.Player) {
            return CollisionLayer.Monster;
        }

        return CollisionLayer.Player;
    }

    /// <summary>
    ///  Determines if the spell can be cast.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="casterId"></param>
    /// <param name="casterPosition"></param>
    /// <param name="enemyLayer"></param>
    /// <param name="range"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    private static bool TryFindNearestEnemy(
        Dimension dimension,
        int casterId,
        Vector3 casterPosition,
        CollisionLayer enemyLayer,
        float range,
        out Vector3 position
    ) {
        position = default;
        var found = false;
        var bestDistSq = range > 0f && float.IsFinite(range) ? range * range : float.MaxValue;

        foreach (var entity in dimension.Query<CollisionComponent>()) {
            var collider = entity.Get<CollisionComponent>();
            if (entity.UniqueId == casterId || collider.Layer != enemyLayer) {
                continue;
            }

            if (!entity.TryGet<LivingComponent>(out var living) || !living.IsAlive) {
                continue;
            }

            var distSq = (entity.Position - casterPosition).LengthSquared();
            if (distSq > bestDistSq) {
                continue;
            }

            bestDistSq = distSq;
            position = entity.Position;
            found = true;
        }

        return found;
    }

    /// <summary>
    ///   Try Resolve Locked Target Entity
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="caster"></param>
    /// <param name="enemyLayer"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private static bool TryResolveLockedTargetEntity(
        Dimension dimension,
        Entity caster,
        CollisionLayer enemyLayer,
        [NotNullWhen(true)] out Entity? target
    ) {
        target = null;
        if (!caster.TryGet<TargetComponent>(out var lockState) || !lockState.EntityId.HasValue) {
            return false;
        }

        var targetId = lockState.EntityId.Value;
        if (!dimension.TryGetEntity(targetId, out target)
            || !target.TryGet<CollisionComponent>(out var collider)
            || collider.Layer != enemyLayer) {
            return false;
        }

        return !target.TryGet<LivingComponent>(out var living) || living.IsAlive;
    }

    /// <summary>
    ///  Determines if the spell can be cast.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private static Vector3 ResolvePlanarVelocity(Entity entity) {
        if (!entity.TryGet<MovementComponent>(out var movement)) {
            return Vector3.Zero;
        }

        return movement.Velocity with { Y = 0f };
    }

    /// <summary>
    ///  Determines if the spell can be cast.
    /// </summary>
    /// <param name="rawDirection"></param>
    /// <returns></returns>
    private static Vector3 ResolvePlanarDirection(Vector3 rawDirection) {
        var flat = rawDirection with { Y = 0f };
        return flat.LengthSquared() > MinDirectionSq
            ? Vector3.Normalize(flat)
            : Vector3.UnitZ;
    }

    /// <summary>
    ///  Determines if the spell can be cast.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="relativeVelocity"></param>
    /// <param name="projectileSpeed"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    private static bool TrySolveInterceptTime(Vector3 offset, Vector3 relativeVelocity, float projectileSpeed,
        out float t) {
        t = 0f;
        if (projectileSpeed <= 0f) {
            return false;
        }

        var a = Vector3.Dot(relativeVelocity, relativeVelocity) - projectileSpeed * projectileSpeed;
        var b = 2f * Vector3.Dot(offset, relativeVelocity);
        var c = Vector3.Dot(offset, offset);

        if (MathF.Abs(a) <= InterceptEpsilon) {
            if (MathF.Abs(b) <= InterceptEpsilon) {
                return false;
            }

            var linear = -c / b;
            if (linear <= 0f || !float.IsFinite(linear)) {
                return false;
            }

            t = linear;
            return true;
        }

        var discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) {
            return false;
        }

        var sqrt = MathF.Sqrt(discriminant);
        var t1 = (-b - sqrt) / (2f * a);
        var t2 = (-b + sqrt) / (2f * a);

        var best = float.MaxValue;
        if (t1 > 0f && float.IsFinite(t1)) {
            best = t1;
        }

        if (t2 > 0f && float.IsFinite(t2) && t2 < best) {
            best = t2;
        }

        if (!float.IsFinite(best) || Math.Abs(best - float.MaxValue) < 0.0001f) {
            return false;
        }

        t = best;
        return true;
    }
}
