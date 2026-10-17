using System.Numerics;
using Yggdrasilnet.Maths.Collision;
using Yggdrasilnet.Network.Enums;
using Yggdrasilnet.Server.Core.Behavior.Models;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Systems.Physics.Steering;
using Yggdrasilnet.Server.Core.World.Content.Spell;
using Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;
using Yggdrasilnet.Server.Core.World.Content.Spell.Effect.Handlers;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;

namespace Yggdrasilnet.Server.Core.Systems.Combat;

public sealed class ProjectileSystem : ISystem {
    private const float MinDirectionSq = 0.0001f;

    private static readonly Dictionary<SpellEffectType, ISpellEffectHandler> EffectHandlers = new() {
        [SpellEffectType.Damage] = new DamageEffectHandler(),
        [SpellEffectType.Heal] = new HealEffectHandler(),
        [SpellEffectType.Dash] = new DashEffectHandler(),
        [SpellEffectType.Punch] = new PunchEffectHandler()
    };

    private readonly BehaviorPackRegistry _behaviors;

    public ProjectileSystem(BehaviorPackRegistry behaviors) {
        _behaviors = behaviors ?? throw new ArgumentNullException(nameof(behaviors));
    }

    public string Id => "projectile";
    public SystemPhase Phase => SystemPhase.Combat;

    public void Update(Dimension dimension, float dt) {
        if (!float.IsFinite(dt) || dt <= 0f) {
            return;
        }

        foreach (var entity in dimension.Query<ProjectileMotionComponent>().ToArray()) {
            var nextMotion = entity.Get<ProjectileMotionComponent>();
            nextMotion.RemainingLifetimeSeconds -= dt;
            if (nextMotion.RemainingLifetimeSeconds <= 0f) {
                dimension.Despawn(entity);
                continue;
            }

            var hasImpact = entity.TryGet<ProjectileImpactComponent>(out var impact);
            if (impact == null) {
                continue;
            }

            nextMotion.Velocity = ResolveSteeringVelocity(dimension, entity, ref nextMotion, hasImpact, impact);

            entity.PreviousPosition = entity.Position;
            entity.Position += nextMotion.Velocity * dt;
            entity.Set(nextMotion);

            if (!hasImpact) {
                continue;
            }

            ResolveHits(dimension, entity, nextMotion, ref impact);
            entity.Set(impact);

            if (impact.RemainingHits <= 0) {
                dimension.Despawn(entity.UniqueId);
            }
        }
    }

    private void ResolveHits(
        Dimension dimension,
        Entity projectile,
        ProjectileMotionComponent motion,
        ref ProjectileImpactComponent impact
    ) {
        if (impact.RemainingHits <= 0) {
            return;
        }

        if (!projectile.TryGet<CollisionComponent>(out var projectileCollider)) {
            return;
        }

        impact.HitEntityIds ??= [];
        var projectileStartBox = projectileCollider.GetWorldAabb(projectile.PreviousPosition);
        var projectileDisplacement = projectile.Position - projectile.PreviousPosition;

        foreach (var target in dimension.Query<LivingComponent>()) {
            if (impact.RemainingHits <= 0) {
                break;
            }

            var living = target.Get<LivingComponent>();
            if (impact.HitEntityIds != null && (!living.IsAlive || target.UniqueId == impact.OwnerEntityId ||
                                                impact.HitEntityIds.Contains(target.UniqueId))) {
                continue;
            }

            if (!target.TryGet<CollisionComponent>(out var collider)
                || (collider.Layer & impact.TargetLayer) == 0) {
                continue;
            }

            var targetStartBox = collider.GetWorldAabb(target.PreviousPosition);
            var relativeDisplacement = projectileDisplacement - (target.Position - target.PreviousPosition);
            if (!AabbSweep.TryGetTimeOfImpact(projectileStartBox, relativeDisplacement, targetStartBox, out _, out _)) {
                continue;
            }

            if (!TryApplyTargetSpellEffects(dimension, projectile, target, impact, motion)) {
                ApplyDamage(target, living, impact.Damage);
            }

            impact.HitEntityIds?.Add(target.UniqueId);
            ApplyAreaDamageOnImpact(dimension, projectile, target.UniqueId, motion, ref impact);
            impact.RemainingHits--;
        }
    }

    private bool TryApplyTargetSpellEffects(
        Dimension dimension,
        Entity projectile,
        Entity target,
        in ProjectileImpactComponent impact,
        in ProjectileMotionComponent motion
    ) {
        return TryApplySpellEffects(dimension, projectile, target, impact, motion, SpellEffectTarget.Target);
    }

    private bool TryApplyAreaSpellEffects(
        Dimension dimension,
        Entity projectile,
        Entity target,
        in ProjectileImpactComponent impact,
        in ProjectileMotionComponent motion
    ) {
        return TryApplySpellEffects(dimension, projectile, target, impact, motion, SpellEffectTarget.Aoe);
    }

    private bool TryApplySpellEffects(
        Dimension dimension,
        Entity projectile,
        Entity target,
        in ProjectileImpactComponent impact,
        in ProjectileMotionComponent motion,
        SpellEffectTarget effectTarget
    ) {
        if (string.IsNullOrWhiteSpace(impact.SpellId)
            || !_behaviors.TryGetSpell(impact.SpellId, out var spellDef)
            || !spellDef.TryGetBehavior<SpellEffectsBehavior>(out var effects)) {
            return false;
        }

        var spellType = spellDef.TryGetBehavior<SpellMetaBehavior>(out var meta) ? meta.ResolvedType : SpellType.Self;
        var context = new SpellCastContext(
            impact.OwnerEntityId,
            impact.SpellId,
            spellType,
            ResolveImpactDirection(projectile, target, motion.Velocity),
            projectile.Position
        );

        var applied = false;
        foreach (var effect in effects.Effects) {
            if (effect.ResolvedTarget != effectTarget || effect.ActiveOnAnticipation) {
                continue;
            }

            if (!EffectHandlers.TryGetValue(effect.ResolvedType, out var handler)) {
                continue;
            }

            handler.Apply(dimension, context, target, effect.Amount, effect.Duration);
            applied = true;
        }

        return applied;
    }

    private static Vector3 ResolveSteeringVelocity(
        Dimension dimension,
        Entity projectile,
        ref ProjectileMotionComponent motion,
        bool hasImpact,
        in ProjectileImpactComponent impact
    ) {
        var direction = ResolvePlanarDirection(motion.Direction, motion.Velocity);
        var speed = ResolvePlanarSpeed(motion);
        if (motion is { AutoTargetEnabled: true, AutoTargetInfluence: > 0f }
            && hasImpact
            && TryResolveAutoTargetDirection(dimension, projectile, impact, motion, out var targetDirection)) {
            var steering = direction + targetDirection * motion.AutoTargetInfluence;
            if (steering.LengthSquared() > SteeringConstants.MinDistanceSquared) {
                direction = Vector3.Normalize(steering);
            }
            else {
                direction = targetDirection;
            }
        }

        motion.Direction = direction;
        motion.Speed = speed;

        return new Vector3(direction.X * speed, motion.Velocity.Y, direction.Z * speed);
    }

    private static bool TryResolveAutoTargetDirection(
        Dimension dimension,
        Entity projectile,
        in ProjectileImpactComponent impact,
        in ProjectileMotionComponent motion,
        out Vector3 targetDirection
    ) {
        targetDirection = default;
        var hasRange = motion.AutoTargetRange > 0f;
        var rangeSq = motion.AutoTargetRange * motion.AutoTargetRange;
        var bestDistanceSq = float.MaxValue;

        foreach (var target in dimension.Query<LivingComponent>()) {
            var living = target.Get<LivingComponent>();
            var uniqueId = target.UniqueId;
            if (!living.IsAlive || uniqueId == impact.OwnerEntityId) {
                continue;
            }

            if (impact.HitEntityIds is { Count: > 0 } hitEntityIds && hitEntityIds.Contains(uniqueId)) {
                continue;
            }

            if (!target.TryGet<CollisionComponent>(out var collider)
                || (collider.Layer & impact.TargetLayer) == 0) {
                continue;
            }

            var toTarget = (target.Position - projectile.Position) with { Y = 0f };
            var distanceSq = toTarget.LengthSquared();
            if (distanceSq <= SteeringConstants.MinDistanceSquared) {
                continue;
            }

            if (hasRange && distanceSq > rangeSq) {
                continue;
            }

            if (distanceSq >= bestDistanceSq) {
                continue;
            }

            bestDistanceSq = distanceSq;
            targetDirection = Vector3.Normalize(toTarget);
        }

        return bestDistanceSq < float.MaxValue;
    }

    private static Vector3 ResolvePlanarDirection(Vector3 preferredDirection, Vector3 velocity) {
        var direction = preferredDirection with { Y = 0f };
        if (direction.LengthSquared() > SteeringConstants.MinDistanceSquared) {
            return Vector3.Normalize(direction);
        }

        var flatVelocity = velocity with { Y = 0f };
        return flatVelocity.LengthSquared() > SteeringConstants.MinDistanceSquared
            ? Vector3.Normalize(flatVelocity)
            : Vector3.UnitZ;
    }

    private static float ResolvePlanarSpeed(in ProjectileMotionComponent motion) {
        if (float.IsFinite(motion.Speed) && motion.Speed > 0f) {
            return motion.Speed;
        }

        var flatVelocity = motion.Velocity with { Y = 0f };
        return MathF.Sqrt(flatVelocity.LengthSquared());
    }

    private void ApplyAreaDamageOnImpact(
        Dimension dimension,
        Entity projectile,
        int primaryTargetId,
        in ProjectileMotionComponent motion,
        ref ProjectileImpactComponent impact
    ) {
        if (!impact.AreaDamageEnabled || impact.AreaDamageRadius <= 0f) {
            return;
        }

        impact.HitEntityIds ??= [];
        var radiusSq = impact.AreaDamageRadius * impact.AreaDamageRadius;

        foreach (var target in dimension.Query<LivingComponent>()) {
            var living = target.Get<LivingComponent>();
            var uniqueId = target.UniqueId;
            if (!living.IsAlive || target.UniqueId == primaryTargetId || target.UniqueId == impact.OwnerEntityId
                || impact.HitEntityIds.Contains(uniqueId)) {
                continue;
            }

            if (!target.TryGet<CollisionComponent>(out var collider)
                || (collider.Layer & impact.TargetLayer) == 0) {
                continue;
            }

            var targetBox = collider.GetWorldAabb(target.Position);
            if (targetBox.DistanceSquaredTo(projectile.Position) > radiusSq) {
                continue;
            }

            if (!TryApplyAreaSpellEffects(dimension, projectile, target, impact, motion) && impact.Damage > 0f) {
                ApplyDamage(target, living, impact.Damage);
            }

            impact.HitEntityIds.Add(target.UniqueId);
        }
    }

    private static Vector3 ResolveImpactDirection(Entity projectile, Entity target, Vector3 velocity) {
        var flatVelocity = velocity with { Y = 0f };
        if (flatVelocity.LengthSquared() > MinDirectionSq) {
            return Vector3.Normalize(flatVelocity);
        }

        var toTarget = (target.Position - projectile.Position) with { Y = 0f };
        return toTarget.LengthSquared() > MinDirectionSq ? Vector3.Normalize(toTarget) : Vector3.UnitZ;
    }

    private static void ApplyDamage(Entity target, LivingComponent living, float amount) {
        living.Current = MathF.Max(0f, living.Current - amount);
        target.Set(living);
    }
}