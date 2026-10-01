using LiteNetLib;
using Yggdrasilnet.Network.Packet.Packets;
using Yggdrasilnet.Network.Packet.Snapshot;

namespace Yggdrasilnet.Server.Services;

public sealed class SnapshotBroadcastService(NetServer netServer, Simulation.Simulation simulation, int tickRate) {
    private const int KeyframeIntervalSeconds = 1;
    private const int SnapshotChunkTargetBytes = 1000;
    private const int SnapshotChunkHeaderBytes = 12;
    private const int SnapshotChunkSafetyMarginBytes = 96;
    private const int SnapshotBaseEntityBytes = EntitySnapshot.HeaderBytes;
    private const int SnapshotVelocityComponentBytes = 13;
    private const int DespawnPacketHeaderBytes = 1 + sizeof(ushort);

    private uint _snapshotFrameId;
    private readonly DespawnEntitiesPacket _despawn = new();

    public SnapshotBroadcastMetrics Broadcast() {
        using var batch = simulation.BeginSnapshotBatch();
        var sessions = simulation.Sessions.All;
        if (sessions.Count == 0) {
            return default;
        }

        var frameId = unchecked(++_snapshotFrameId);
        var keyframeIntervalTicks = Math.Max(1, tickRate * KeyframeIntervalSeconds);
        var isKeyframeTick = simulation.Tick % keyframeIntervalTicks == 0;
        var metrics = new SnapshotBroadcastMetrics {
            Sessions = sessions.Count,
            Keyframe = isKeyframeTick
        };

        foreach (var session in sessions) {
            var snapshot = batch.Build(session, tickRate, isKeyframeTick);
            SendRemovedEntities(session.Peer, batch.RemovedEntityIds, ref metrics);
            metrics.SnapshotEntities += snapshot.Entities.Count;
            if (snapshot.Entities.Count == 0) {
                continue;
            }

            SendSnapshotChunks(session.Peer, snapshot.Entities, frameId, isKeyframeTick, ref metrics);
        }

        return metrics;
    }

    private void SendRemovedEntities(NetPeer peer, IReadOnlyList<int> entityIds, ref SnapshotBroadcastMetrics metrics) {
        if (entityIds.Count == 0) {
            return;
        }

        const DeliveryMethod delivery = DeliveryMethod.ReliableOrdered;
        var targetBytes = ResolveChunkTargetBytes(peer, delivery);
        var maxIdsPerPacket = Math.Max(1, (targetBytes - DespawnPacketHeaderBytes) / sizeof(int));
        _despawn.EntityIds.Clear();
        try {
            for (var i = 0; i < entityIds.Count; i++) {
                _despawn.EntityIds.Add(entityIds[i]);
                if (_despawn.EntityIds.Count < maxIdsPerPacket && i < entityIds.Count - 1) {
                    continue;
                }

                netServer.Send(peer, _despawn);
                metrics.DespawnPacketsSent++;
                metrics.DespawnEntities += _despawn.EntityIds.Count;
                _despawn.EntityIds.Clear();
            }
        } finally {
            _despawn.EntityIds.Clear();
        }
    }

    private void SendSnapshotChunks(NetPeer peer, List<EntitySnapshot> entities, uint frameId, bool keyframeTick, ref SnapshotBroadcastMetrics metrics) {
        if (entities.Count == 0) {
            return;
        }

        var delivery = keyframeTick ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable;
        var chunkTargetBytes = ResolveChunkTargetBytes(peer, delivery);
        var chunkingResult = SnapshotChunker.Split(
            frameId,
            keyframeTick,
            entities,
            chunkTargetBytes,
            SnapshotChunkHeaderBytes,
            EstimateEntityBytes
        );

        metrics.DroppedOversizedEntities += chunkingResult.DroppedOversizedEntities;
        if (chunkingResult.DroppedFrame) {
            metrics.DroppedFrames++;
            return;
        }

        foreach (var chunk in chunkingResult.Chunks) {
            SendChunk(peer, chunk, delivery, ref metrics);
        }
    }

    private static int ResolveChunkTargetBytes(NetPeer peer, DeliveryMethod delivery) {
        var maxPacketSize = peer.GetMaxSinglePacketSize(delivery);
        var safeTarget = maxPacketSize - SnapshotChunkSafetyMarginBytes;
        if (safeTarget <= SnapshotChunkHeaderBytes) {
            return SnapshotChunkHeaderBytes + 1;
        }

        return Math.Min(SnapshotChunkTargetBytes, safeTarget);
    }

    private void SendChunk(NetPeer peer, SnapshotChunkPacket chunk, DeliveryMethod delivery, ref SnapshotBroadcastMetrics metrics) {
        netServer.Send(peer, chunk, delivery);
        metrics.ChunksSent++;
    }

    private static int EstimateEntityBytes(EntitySnapshot entity) {
        if (entity.EstimatedBytes > 0) {
            return entity.EstimatedBytes;
        }

        return SnapshotBaseEntityBytes + entity.Components.Sum(component => component.Type switch {
            NetworkedComponentType.Velocity => SnapshotVelocityComponentBytes,
            _ => 0
        });
    }
}

public struct SnapshotBroadcastMetrics {
    public int Sessions { get; set; }
    public int SnapshotEntities { get; set; }
    public int ChunksSent { get; set; }
    public int DroppedOversizedEntities { get; set; }
    public int DespawnPacketsSent { get; set; }
    public int DespawnEntities { get; set; }
    public bool Keyframe { get; set; }
    public int DroppedFrames { get; set; }
}
