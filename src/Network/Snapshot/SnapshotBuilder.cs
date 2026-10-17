using System.Numerics;
using System.Text;
using Yggdrasilnet.Network.Enums;
using Yggdrasilnet.Network.Snapshot;
using Yggdrasilnet.Network.Snapshot.Components;
using Yggdrasilnet.Server.Core.Registry;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Combat;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;
using TargetComponent = Yggdrasilnet.Network.Snapshot.Components.TargetComponent;
using WorldCollisionComponent = Yggdrasilnet.Server.Core.World.Entities.Components.Collision.CollisionComponent;
using WorldTargetComponent = Yggdrasilnet.Server.Core.World.Entities.Components.TargetComponent;

namespace Yggdrasilnet.Server.Network.Snapshot;

/// <summary>
///  Builds entity snapshots from the world state.
/// </summary>
public sealed class SnapshotBuilder {
    private const int SnapshotVelocityComponentBytes = 13;
    private const int SnapshotHealthComponentBytes = 9;
    private const int SnapshotCollisionComponentBytes = 21;
    private const int SnapshotDirectionComponentBytes = 8;
    private const int SnapshotTargetComponentBytes = 25;

    private readonly EntityRegistry _entityRegistry;

    public SnapshotBuilder(EntityRegistry entityRegistry) {
        _entityRegistry = entityRegistry;
    }

    /// <summary>
    ///  Builds the snapshots of every entity of the given dimension.
    /// </summary>
    /// <param name="dimension">The dimension to snapshot.</param>
    /// <param name="ownerEntityId">The entity owned by the receiving session.</param>
    /// <param name="ownerLastInputSequence">The last input sequence acknowledged for the owner entity.</param>
    /// <returns></returns>
    public List<EntitySnapshot> Build(IDimension dimension, int ownerEntityId, uint ownerLastInputSequence) {
        var snapshots = new List<EntitySnapshot>();

        foreach (var entity in dimension.GetAllEntities()) {
            var snapshot = new EntitySnapshot {
                EntityId = entity.UniqueId,
                PositionX = entity.Position.X,
                PositionY = entity.Position.Y,
                PositionZ = entity.Position.Z,
                DefinitionId = _entityRegistry.EnsureDefinitionId(entity.Id),
                LastInputSequence = entity.UniqueId == ownerEntityId ? ownerLastInputSequence : 0
            };

            AddComponents(entity, snapshot.Components);

            snapshot.EstimatedBytes = EstimateEntityBytes(snapshot);
            snapshots.Add(snapshot);
        }

        return snapshots;
    }

    /// <summary>
    ///  Estimates the serialized size of an entity snapshot.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static int EstimateEntityBytes(EntitySnapshot entity) {
        if (entity.EstimatedBytes > 0) {
            return entity.EstimatedBytes;
        }

        var bytes = EntitySnapshot.HeaderBytes;
        foreach (var component in entity.Components) {
            bytes += EntitySnapshot.ComponentHeaderBytes;
            bytes += component.Type switch {
                NetworkedComponentType.Velocity => SnapshotVelocityComponentBytes,
                NetworkedComponentType.Health => SnapshotHealthComponentBytes,
                NetworkedComponentType.Collision => SnapshotCollisionComponentBytes,
                NetworkedComponentType.Direction => SnapshotDirectionComponentBytes,
                NetworkedComponentType.Target => SnapshotTargetComponentBytes,
                NetworkedComponentType.Action when component is ActionComponent action =>
                    15 + Encoding.UTF8.GetByteCount(action.SpellId),
                NetworkedComponentType.Projectile when component is ProjectileComponent projectile =>
                    11 + Encoding.UTF8.GetByteCount(projectile.SpellId),
                _ => 0
            };
        }

        return bytes;
    }

    /// <summary>
    ///  Adds the components of an entity to the snapshot.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="components"></param>
    private static void AddComponents(Entity entity, List<INetworkedComponent> components) {
        if (TryGetVelocity(entity, out var velocity)) {
            components.Add(new VelocityComponent {
                X = velocity.X,
                Y = velocity.Y,
                Z = velocity.Z
            });

            var xzLengthSq = velocity.X * velocity.X + velocity.Z * velocity.Z;
            if (xzLengthSq > 1e-8f) {
                var invLength = 1f / MathF.Sqrt(xzLengthSq);
                components.Add(new DirectionComponent {
                    X = velocity.X * invLength,
                    Z = velocity.Z * invLength
                });
            }
        }

        if (entity.TryGet<LivingComponent>(out var living)) {
            components.Add(new HealthComponent {
                Current = living.Current,
                Max = living.Max
            });
        }

        if (entity.TryGet<WorldCollisionComponent>(out var collision)) {
            components.Add(new CollisionComponent {
                X = collision.X,
                Y = collision.Y,
                Z = collision.Z,
                IsTrigger = collision.IsTrigger,
                Layer = (CollisionLayer)(uint)collision.Layer,
                Mask = (CollisionLayer)(uint)collision.Mask
            });
        }

        if (entity.TryGet<CombatComponent>(out var combat)) {
            components.Add(combat.IsCasting
                ? new ActionComponent {
                    ActionType = 1,
                    Phase = combat.CastPhase,
                    PhaseProgress01 = combat.PhaseDuration > 0f
                        ? Math.Clamp(1f - combat.PhaseTimer / combat.PhaseDuration, 0f, 1f)
                        : 1f,
                    SpellId = combat.CastSpellId,
                    TargetEntityId = -1,
                    SpellType = combat.CastSpellType,
                    HasDashEffect = combat.CastHasDashEffect
                }
                : new ActionComponent {
                    ActionType = 0,
                    Phase = SpellPhase.None,
                    PhaseProgress01 = 0f,
                    SpellId = string.Empty,
                    TargetEntityId = -1
                });
        }

        if (entity.TryGet<WorldTargetComponent>(out var target)) {
            components.Add(new TargetComponent {
                EntityId = target.EntityId,
                Distance = target.Distance,
                Direction = target.Direction,
                HealthRatio = target.HealthRatio
            });
        }
        else if (entity.TryGet<BrainComponent>(out var brain)) {
            components.Add(new TargetComponent {
                EntityId = brain.TargetEntityId,
                Distance = brain.TargetDistance,
                Direction = brain.TargetDirection,
                HealthRatio = brain.TargetHealthRatio
            });
        }

        if (entity.TryGet<ProjectileImpactComponent>(out var impact)) {
            components.Add(new ProjectileComponent {
                SpellId = impact.SpellId,
                CasterEntityId = impact.OwnerEntityId,
                TargetEntityId = -1
            });
        }
    }

    private static bool TryGetVelocity(Entity entity, out Vector3 velocity) {
        if (entity.TryGet<MovementComponent>(out var movement)) {
            velocity = movement.Velocity;
            return true;
        }

        if (entity.TryGet<ProjectileMotionComponent>(out var projectileMotion)) {
            velocity = projectileMotion.Velocity;
            return true;
        }

        velocity = default;
        return false;
    }
}
