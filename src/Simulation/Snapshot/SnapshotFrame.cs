using Yggdrasilnet.Network.Packet.Snapshot;

namespace Yggdrasilnet.Server.Simulation.Snapshot;

public sealed class SnapshotFrame {
    public List<EntitySnapshot> Entities { get; } = [];
}