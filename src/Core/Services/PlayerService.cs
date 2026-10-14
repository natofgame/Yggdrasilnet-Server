using System.Numerics;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Core.Events.Events;
using Yggdrasilnet.Server.Core.Registry;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Core.Services;

public sealed class PlayerService(
    IWorld world,
    SessionRegistry sessions,
    NetworkService network,
    EntityRegistry entityRegistry) {
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

    public void ApplyInput(GameSession session, float x, float z, uint sequence) {
        if (!session.ApplyInput(x, z, sequence)) {
            return;
        }

        if (!session.TryGetEntityBinding(out var entityId, out var dimensionName)) {
            return;
        }

        world.TrySetInput(dimensionName, entityId, x, z);
    }

    public void SyncSessionsFromWorld() {
        foreach (var session in sessions.All) {
            if (session.Phase != ConnectionPhase.Game) {
                continue;
            }

            if (!session.TryGetEntityBinding(out var entityId, out var dimensionName)) {
                continue;
            }

            if (!world.TryGetEntity(dimensionName, entityId, out var entity) || entity is null) {
                continue;
            }

            session.SyncPosition(entity.Position.X, entity.Position.Z);
        }
    }
}
