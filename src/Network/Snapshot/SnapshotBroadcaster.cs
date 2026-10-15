using System.Numerics;
using System.Text;
using LiteNetLib;
using Serilog;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.NetworkV4.Snapshot;
using Yggdrasilnet.NetworkV4.Snapshot.Components;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Combat;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;
using Yggdrasilnet.Server.Network.Session;
using TargetComponent = Yggdrasilnet.NetworkV4.Snapshot.Components.TargetComponent;
using WorldCollisionComponent = Yggdrasilnet.Server.Core.World.Entities.Components.Collision.CollisionComponent;
using WorldTargetComponent = Yggdrasilnet.Server.Core.World.Entities.Components.TargetComponent;

namespace Yggdrasilnet.Server.Network.Snapshot;

// TODO: Split builder and broadcaster
// TODO: Refacto
public class SnapshotBroadcaster {
    private const int SnapshotChunkTargetBytes = 1000;
    private const int SnapshotChunkHeaderBytes = 12;
    private const int KeyframeIntervalSeconds = 1;

    private const int SnapshotVelocityComponentBytes = 13;
    private const int SnapshotHealthComponentBytes = 9;
    private const int SnapshotCollisionComponentBytes = 21;
    private const int SnapshotDirectionComponentBytes = 8;
    private const int SnapshotTargetComponentBytes = 25;

    private readonly SessionRegistry _sessions;
    private readonly Simulation _simulation;
    private readonly int _tickRate;
    private readonly IWorld _world;

    private uint _snapshotFrameId;

    public SnapshotBroadcaster(SessionRegistry session, IWorld world, Simulation simulation, int tickRate) {
        _sessions = session;
        _world = world;

        _tickRate = Math.Max(1, tickRate);
        _simulation = simulation;
    }

    public void BroadcastSnapshots(long tick) {
        var sessions = _sessions.All;
        if (sessions.Count == 0) {
            return;
        }

        var frameId = unchecked(++_snapshotFrameId);
        var keyframeIntervalTicks = Math.Max(1, _tickRate * KeyframeIntervalSeconds);
        var isKeyframe = tick % keyframeIntervalTicks == 0;
        var frameDelivery = isKeyframe ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable;

        foreach (var session in sessions) {
            if (session.Phase != ConnectionPhase.Game) {
                continue;
            }

            if (!session.TryGetEntityBinding(out var ownerEntityId, out var dimensionName)) {
                continue;
            }

            if (!_world.TryGetDimension(dimensionName, out var dimension) || dimension is null) {
                continue;
            }

            var snapshots = BuildSnapshots(session, dimension, ownerEntityId, out var visibleEntities);
            SendDespawn(session, visibleEntities);

            var registry = _simulation.EntityRegistry;
            if (session.SnapshotDefinitionsVersion < registry.DefinitionVersion) {
                registry.SendDefinitions(session);
            }

            if (snapshots.Count == 0) {
                continue;
            }

            var chunking = SnapshotChunker.Split(
                frameId,
                isKeyframe,
                snapshots,
                SnapshotChunkTargetBytes,
                SnapshotChunkHeaderBytes,
                EstimateEntityBytes
            );

            if (chunking.DroppedFrame) {
                Log.Warning("Snapshot frame {FrameId} dropped for {EndPoint} because too many chunks.",
                    frameId, session.Peer.Address);
                continue;
            }

            var delivery = session.SnapshotKnownEntities.Count == 0
                ? DeliveryMethod.ReliableOrdered
                : frameDelivery;

            foreach (var chunk in chunking.Chunks)
                _simulation.Network.Send(session, chunk, delivery);
        }
    }

    private List<EntitySnapshot> BuildSnapshots(GameSession session, IDimension world, int ownerEntityId,
        out HashSet<int> visibleEntities) {
        visibleEntities = [];
        var snapshots = new List<EntitySnapshot>();

        foreach (var entity in world.GetAllEntities()) {
            visibleEntities.Add(entity.UniqueId);

            var snapshot = new EntitySnapshot {
                EntityId = entity.UniqueId,
                PositionX = entity.Position.X,
                PositionY = entity.Position.Y,
                PositionZ = entity.Position.Z,
                DefinitionId = _simulation.EntityRegistry.EnsureDefinitionId(entity.Id),
                LastInputSequence = entity.UniqueId == ownerEntityId ? session.LastSequence : 0
            };

            if (TryGetSnapshotVelocity(world, entity.UniqueId, out var velocity)) {
                snapshot.Components.Add(new VelocityComponent {
                    X = velocity.X,
                    Y = velocity.Y,
                    Z = velocity.Z
                });

                var xzLengthSq = velocity.X * velocity.X + velocity.Z * velocity.Z;
                if (xzLengthSq > 1e-8f) {
                    var invLength = 1f / MathF.Sqrt(xzLengthSq);
                    snapshot.Components.Add(new DirectionComponent {
                        X = velocity.X * invLength,
                        Z = velocity.Z * invLength
                    });
                }
            }

            if (entity.TryGet<LivingComponent>(out var living)) {
                snapshot.Components.Add(new HealthComponent {
                    Current = living.Current,
                    Max = living.Max
                });
            }

            if (entity.TryGet<WorldCollisionComponent>(out var collision)) {
                snapshot.Components.Add(new CollisionComponent {
                    X = collision.X,
                    Y = collision.Y,
                    Z = collision.Z,
                    IsTrigger = collision.IsTrigger,
                    Layer = (CollisionLayer)(uint)collision.Layer,
                    Mask = (CollisionLayer)(uint)collision.Mask
                });
            }

            if (entity.TryGet<CombatComponent>(out var combat)) {
                snapshot.Components.Add(combat.IsCasting
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
                snapshot.Components.Add(new TargetComponent {
                    EntityId = target.EntityId,
                    Distance = target.Distance,
                    Direction = target.Direction,
                    HealthRatio = target.HealthRatio
                });
            }
            else if (entity.TryGet<BrainComponent>(out var brain)) {
                snapshot.Components.Add(new TargetComponent {
                    EntityId = brain.TargetEntityId,
                    Distance = brain.TargetDistance,
                    Direction = brain.TargetDirection,
                    HealthRatio = brain.TargetHealthRatio
                });
            }

            if (entity.TryGet<ProjectileImpactComponent>(out var impact)) {
                snapshot.Components.Add(new ProjectileComponent {
                    SpellId = impact.SpellId,
                    CasterEntityId = impact.OwnerEntityId,
                    TargetEntityId = -1
                });
            }

            snapshot.EstimatedBytes = EstimateEntityBytes(snapshot);
            snapshots.Add(snapshot);
        }

        return snapshots;
    }

    private void SendDespawn(GameSession session, HashSet<int> visibleEntities) {
        if (session.SnapshotKnownEntities.Count == 0) {
            foreach (var visibleEntity in visibleEntities)
                session.SnapshotKnownEntities.Add(visibleEntity);
            return;
        }

        List<int>? removed = null;
        foreach (var knownEntity in session.SnapshotKnownEntities.Where(knownEntity =>
                     !visibleEntities.Contains(knownEntity))) {
            removed ??= [];
            removed.Add(knownEntity);
        }

        if (removed is { Count: > 0 }) {
            var packet = new DespawnEntitiesPacket();
            foreach (var id in removed)
                packet.EntityIds.Add(id);

            _simulation.Network.Send(session, packet);
        }

        session.SnapshotKnownEntities.Clear();
        foreach (var visibleEntity in visibleEntities)
            session.SnapshotKnownEntities.Add(visibleEntity);
    }

    private static bool TryGetSnapshotVelocity(IDimension world, int entityId, out Vector3 velocity) {
        if (world.TryGetEntity(entityId, out var entity)) {
            if (entity.TryGet<MovementComponent>(out var movement)) {
                velocity = movement.Velocity;
                return true;
            }

            if (entity.TryGet<ProjectileMotionComponent>(out var projectileMotion)) {
                velocity = projectileMotion.Velocity;
                return true;
            }
        }

        velocity = default;
        return false;
    }

    private static int EstimateEntityBytes(EntitySnapshot entity) {
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
}