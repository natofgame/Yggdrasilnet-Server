using System.Numerics;
using Yggdrasilnet.Network.Enums;
using Yggdrasilnet.Server.Simulation.World.Component;

namespace Yggdrasilnet.Server.Simulation.World.System;

public sealed class TargetingSystem : ISystem {
    public void Update(World world, float dt) {
        foreach (var (entity, target) in world.Query<TargetComponent>()) {
            Tick(world, entity, target, dt);
        }
    }

    private void Tick(World world, Entity self, TargetComponent target, float dt) {
        if (!self.TryGetComponent<CollisionComponent>(out var selfCollider)) {
            ClearTarget(target);
            return;
        }

        var enemyLayer = OpposingLayer(selfCollider.Layer);

        target.RefreshTimer -= dt;
        if (target.RefreshTimer > 0f) {
            return;
        }

        target.RefreshTimer = target.RefreshInterval;

        if (TryFindNearest(world, self, target.Range, enemyLayer, out var found)) {
            ApplyTarget(self, target, found);
            return;
        }

        ClearTarget(target);
    }

    private static CollisionLayer OpposingLayer(CollisionLayer selfLayer) {
        return selfLayer == CollisionLayer.Player ? CollisionLayer.Monster : CollisionLayer.Player;
    }

    private bool TryFindNearest(World world, Entity self, float range, CollisionLayer enemyLayer, out Entity nearest) {
        nearest = null!;
        var bestDistSq = range * range;
        var found = false;

        foreach (var (other, collider) in world.Query<CollisionComponent>()) {
            if (other == self || collider.Layer != enemyLayer) {
                continue;
            }

            if (other.TryGetComponent<HealthComponent>(out var hp) && hp.Current <= 0f) {
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

    private void ApplyTarget(Entity self, TargetComponent target, Entity found) {
        target.EntityId = found.Id;

        var offset = found.Position - self.Position;
        var distance = offset.Length();
        target.Distance = distance;
        target.Direction = distance > 0.0001f ? offset / distance : Vector3.Zero;

        target.HealthRatio = found.TryGetComponent<HealthComponent>(out var hp) && hp.Max > 0f
            ? Math.Clamp(hp.Current / hp.Max, 0f, 1f)
            : 1f;
    }

    private void ClearTarget(TargetComponent target) {
        target.EntityId = null;
        target.Distance = 0f;
        target.Direction = Vector3.Zero;
        target.HealthRatio = 0f;
    }
}