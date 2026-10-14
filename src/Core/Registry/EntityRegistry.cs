using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Core.Registry;

public class EntityRegistry {
    private readonly Dictionary<string, byte> _definitionIds = new(StringComparer.OrdinalIgnoreCase);

    private readonly Simulation _simulation;

    public EntityRegistry(Simulation simulation) {
        _simulation = simulation;
    }

    public int DefinitionVersion { get; private set; }

    public void SendDefinitions(GameSession session) {
        if (_definitionIds.Count == 0) {
            return;
        }

        var packet = new EntityDefinitionsPacket();
        foreach (var (definitionId, id) in _definitionIds.OrderBy(entry => entry.Value))
            packet.Definitions.Add(new EntityDefinitionEntry(id, definitionId));

        _simulation.Network.Send(session, packet);
        session.SnapshotDefinitionsVersion = DefinitionVersion;
    }

    public byte EnsureDefinitionId(string definitionId) {
        if (_definitionIds.TryGetValue(definitionId, out var existing)) {
            return existing;
        }

        if (_definitionIds.Count > byte.MaxValue) {
            throw new InvalidOperationException("Too many entity definitions for snapshot protocol (max 256).");
        }

        var created = (byte)_definitionIds.Count;
        _definitionIds[definitionId] = created;
        DefinitionVersion++;
        return created;
    }
}
