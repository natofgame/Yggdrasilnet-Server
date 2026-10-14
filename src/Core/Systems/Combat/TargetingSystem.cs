using System.Numerics;
using System.Runtime.CompilerServices;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;

namespace Yggdrasilnet.Server.Core.Systems.Combat;

/// <summary>
///  Handles targeting of entities.
/// </summary>
public sealed class TargetingSystem : ISystem {
    public const string Identifier = "targeting";
    private const float MinDirectionSq = 0.0001f;
    
    /// <inheritdoc/>
    public string Id => Identifier;
    
    /// <inheritdoc/>
    public SystemPhase Phase => SystemPhase.Combat;

    /// <inheritdoc/>
    public void Update(Dimension dimension, float dt) {
        if (!float.IsFinite(dt) || dt < 0f) {
            return;
        }

        foreach (var entity in dimension.Query<CollisionComponent>().ToArray()) {
            var collider = entity.Get<CollisionComponent>();
            if (collider.Layer != CollisionLayer.Player && collider.Layer != CollisionLayer.Monster) {
                continue;
            }

            if (!entity.TryGet<TargetComponent>(out var target)) {
                target = TargetComponent.CreateDefault();
            }

            Tick(dimension, entity, collider, ref target, dt);
            entity.Set(target);
        }
    }

    /// <summary>
    ///  Tick the targeting system.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="self"></param>
    /// <param name="selfCollider"></param>
    /// <param name="target"></param>
    /// <param name="dt"></param>
    private static void Tick(
        Dimension dimension,
        Entity self,
        CollisionComponent selfCollider,
        ref TargetComponent target,
        float dt
    ) {
        if (target.Range <= 0f) {
            target.Range = 20f;
        }

        if (target.RefreshInterval <= 0f) {
            target.RefreshInterval = 0.2f;
        }

        if (self.TryGet<LivingComponent>(out var selfHealth) && !selfHealth.IsAlive) {
            ClearTarget(ref target);
            return;
        }

        var enemyLayer = OpposingLayer(selfCollider.Layer);
        target.RefreshTimer -= dt;
        if (target.RefreshTimer > 0f) {
            return;
        }

        target.RefreshTimer = target.RefreshInterval;

        if (TryFindNearest(dimension, self, target.Range, enemyLayer, out var found)) {
            ApplyTarget(self, ref target, found);
            return;
        }

        ClearTarget(ref target);
    }

    /// <summary>
    ///  Returns the opposing layer for the given layer.
    /// </summary>
    /// <param name="selfLayer"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CollisionLayer OpposingLayer(CollisionLayer selfLayer) {
        return selfLayer == CollisionLayer.Player ? CollisionLayer.Monster : CollisionLayer.Player;
    }

    /// <summary>
    ///  Tries to find the nearest entity in the given range.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="self"></param>
    /// <param name="range"></param>
    /// <param name="enemyLayer"></param>
    /// <param name="nearest"></param>
    /// <returns></returns>
    private static bool TryFindNearest(
        Dimension dimension,
        Entity self,
        float range,
        CollisionLayer enemyLayer,
        out Entity nearest
    ) {
        nearest = null!;
        var bestDistSq = range * range;
        var found = false;

        foreach (var other in dimension.Query<CollisionComponent>()) {
            var collider = other.Get<CollisionComponent>();
            if (other.UniqueId == self.UniqueId || collider.Layer != enemyLayer) {
                continue;
            }

            if (other.TryGet<LivingComponent>(out var hp) && !hp.IsAlive) {
                continue;
            }

            var distSq = Vector3.DistanceSquared(self.Position, other.Position);
            if (distSq > bestDistSq) {
                continue;
            }

            bestDistSq = distSq;
            nearest = other;
            found = true;
        }

        return found;
    }

    /// <summary>
    ///   Applies the target to the entity.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="target"></param>
    /// <param name="found"></param>
    private static void ApplyTarget(
        Entity self,
        ref TargetComponent target,
        Entity found
    ) {
        target.EntityId = found.UniqueId;

        var offset = found.Position - self.Position;
        var distance = offset.Length();
        target.Distance = distance;
        target.Direction = distance > MinDirectionSq ? offset / distance : Vector3.Zero;

        target.HealthRatio = self.TryGet<LivingComponent>(out var hp) && hp.Max > 0f
            ? Math.Clamp(hp.Current / hp.Max, 0f, 1f)
            : 1f;
    }

    /// <summary>
    ///  Clears the target.
    /// </summary>
    /// <param name="target"></param>
    private static void ClearTarget(ref TargetComponent target) {
        target.EntityId = null;
        target.Distance = 0f;
        target.Direction = Vector3.Zero;
        target.HealthRatio = 0f;
    }
}
