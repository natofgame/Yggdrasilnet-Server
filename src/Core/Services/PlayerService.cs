using System.Numerics;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Core.Events.Events;
using Yggdrasilnet.Server.Core.Registry;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Core.Services;

/// <summary>
///  Service for managing players.
/// </summary>
/// <param name="world"></param>
/// <param name="network"></param>
/// <param name="entityRegistry"></param>
public sealed class PlayerService(
    IWorld world,
    NetworkService network,
    EntityRegistry entityRegistry) {
    
    /// <summary>
    ///  Creates a new player entity and binds it to the given session.
    /// </summary>
    /// <param name="session"></param>
    public void EnterGame(GameSession session) {
        if (session.ControlledEntityId is not null) {
            return;
        }

        var player = world.Spawn("player", Vector3.Zero);
        session.BindEntity(player.UniqueId, world.MainDimension.Id);
        session.MarkReady();

        if (world.TryResolveEntity(player.UniqueId, out var entity) && entity is not null) {
            entityRegistry.EnsureDefinitionId(entity.Id);
        }

        network.Send(session, new PlayerConnexionPacket {
            PlayerId = session.Peer.Id,
            IsOwner = true,
            EntityId = player.UniqueId
        });

        entityRegistry.SendDefinitions(session);

        world.Events.Post(new PlayerConnexionEvent(
            session.Peer.Address.ToString(),
            player.UniqueId));
    }

    /// <summary>
    ///  Applies the given input to the player entity.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="sequence"></param>
    public void ApplyInput(GameSession session, float x, float z, uint sequence) {
        if (!session.ApplyInput(x, z, sequence)) {
            return;
        }

        if (!session.TryGetEntityBinding(out var entityId, out var dimensionName)) {
            return;
        }

        world.TrySetInput(dimensionName, entityId, x, z);
    }
}
