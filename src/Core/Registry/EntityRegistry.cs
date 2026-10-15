using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Core.Registry;

/// <summary>
///     Registry for entities.
/// </summary>
/// <param name="simulation"></param>
public class EntityRegistry(Simulation simulation) {
    private readonly Dictionary<string, byte> _definitionIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     The current definition version.
    /// </summary>
    public int DefinitionVersion { get; private set; }

    /// <summary>
    ///     Sends the entity definitions to the given session.
    /// </summary>
    /// <param name="session"></param>
    public void SendDefinitions(GameSession session) {
        if (_definitionIds.Count == 0) {
            return;
        }

        var packet = new EntityDefinitionsPacket();
        foreach (var (definitionId, id) in _definitionIds.OrderBy(entry => entry.Value))
            packet.Definitions.Add(new EntityDefinitionEntry(id, definitionId));

        simulation.Network.Send(session, packet);
        session.SnapshotDefinitionsVersion = DefinitionVersion;
    }

    /// <summary>
    ///     Ensures that the given definition ID is registered.
    /// </summary>
    /// <param name="definitionId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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