using LiteNetLib;
using Serilog;
using Yggdrasilnet.Network.Packets;
using Yggdrasilnet.Network.Snapshot;
using Yggdrasilnet.Server.Configuration;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Network.Snapshot;

/// <summary>
///  Sends the snapshots built by the <see cref="SnapshotBuilder"/> to every in-game session.
/// </summary>
public class SnapshotBroadcaster {
    private readonly SnapshotBuilder _builder;
    private readonly int _chunkTargetBytes;
    private readonly int _keyframeIntervalTicks;
    private readonly SessionRegistry _sessions;
    private readonly Simulation _simulation;
    private readonly IWorld _world;

    private uint _snapshotFrameId;

    /// <summary>
    ///  Creates a new <see cref="SnapshotBroadcaster"/>.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="world"></param>
    /// <param name="simulation"></param>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <param name="tickRate"></param>
    public SnapshotBroadcaster(SessionRegistry session, IWorld world, Simulation simulation, SnapshotBuilder builder,
        SnapshotConfiguration configuration, int tickRate) {
        if (configuration.ChunkTargetBytes <= SnapshotChunkPacket.HeaderBytes) {
            throw new ArgumentOutOfRangeException(nameof(configuration),
                $"snapshot.chunkTargetBytes must be greater than {SnapshotChunkPacket.HeaderBytes}.");
        }

        _sessions = session;
        _world = world;
        _builder = builder;
        _simulation = simulation;

        _chunkTargetBytes = configuration.ChunkTargetBytes;
        _keyframeIntervalTicks = Math.Max(1,
            (int)MathF.Round(Math.Max(1, tickRate) * configuration.KeyframeIntervalSeconds));
    }

    /// <summary>
    ///  Broadcasts the snapshots to all in-game sessions.
    /// </summary>
    /// <param name="tick"></param>
    public void BroadcastSnapshots(long tick) {
        var sessions = _sessions.All;
        if (sessions.Count == 0) {
            return;
        }

        var frameId = unchecked(++_snapshotFrameId);
        var isKeyframe = tick % _keyframeIntervalTicks == 0;
        var frameDelivery = isKeyframe ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable;

        foreach (var session in sessions) {
            if (session.Phase != ConnectionPhase.Game) {
                continue;
            }

            if (!session.TryGetEntityBinding(out var ownerEntityId, out var dimensionName)) {
                continue;
            }

            if (!_world.TryGetDimension(dimensionName, out var dimension)) {
                continue;
            }

            var snapshots = _builder.Build(dimension, ownerEntityId, session.LastSequence);
            SendDespawn(session, snapshots);

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
                _chunkTargetBytes,
                SnapshotChunkPacket.HeaderBytes,
                SnapshotBuilder.EstimateEntityBytes
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

    /// <summary>
    ///  Sends the despawn packets to the given session.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="snapshots"></param>
    private void SendDespawn(GameSession session, List<EntitySnapshot> snapshots) {
        var visibleEntities = new HashSet<int>(snapshots.Count);
        foreach (var snapshot in snapshots)
            visibleEntities.Add(snapshot.EntityId);

        if (session.SnapshotKnownEntities.Count > 0) {
            var packet = new DespawnEntitiesPacket();
            foreach (var knownEntity in session.SnapshotKnownEntities.Where(knownEntity => !visibleEntities.Contains(knownEntity))) {
                packet.EntityIds.Add(knownEntity);
            }

            if (packet.EntityIds.Count > 0) {
                _simulation.Network.Send(session, packet);
            }
        }

        session.SnapshotKnownEntities.Clear();
        session.SnapshotKnownEntities.UnionWith(visibleEntities);
    }
}
