using System.Numerics;
using Yggdrasilnet.Network.Packets;
using Yggdrasilnet.Server.Core.Registry;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Event.Events.Player;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Core.Services;

/// <summary>
///     Service for managing players.
/// </summary>
/// <param name="world"></param>
/// <param name="network"></param>
/// <param name="entityRegistry"></param>
public sealed class PlayerService(
    Bootstrap.Server server,
    IWorld world,
    NetworkService network,
    EntityRegistry entityRegistry) {
    
    private readonly Dictionary<string, GameSession> _names = new(StringComparer.OrdinalIgnoreCase);
    
    public bool TryLogin(GameSession session, string? requestedName, out string error) {
        if (!Utils.PlayerNameValidator.TryNormalize(requestedName, out var name, out error)) {
            return false;
        }

        if (!_names.TryAdd(name, session)) {
            error = "Name already in use.";
            return false;
        }

        session.SetProfile(new PlayerProfile(Guid.NewGuid(), name));
        return true;
    }
    
    /// <summary>
    ///     Frees the name reserved by the given session, if it still owns it.
    /// </summary>
    public void ReleaseName(GameSession session) {
        if (session.Profile is { } profile
            && _names.TryGetValue(profile.Name, out var owner)
            && ReferenceEquals(owner, session)) {
            _names.Remove(profile.Name);
        }
    }
    
    /// <summary>
    ///     Creates a new player entity and binds it to the given session.
    /// </summary>
    /// <param name="session"></param>
    public void EnterGame(GameSession session) {
        if (session.ControlledEntityId is not null) {
            return;
        }
        
        var spawned = world.Spawn("player", Vector3.Zero);
        spawned.Set(new NameComponent { Value = session.Profile!.Name });
        var player = spawned.ToPlayer();
        
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

        server.Event.Publish(new PlayerJoinEvent(player));
    }

    /// <summary>
    ///     Applies the given input to the player entity.
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
        if (!float.IsFinite(x) || !float.IsFinite(z)) {
            return;
        }

        var dir = new Vector2(x, z);
        if (dir.LengthSquared() > 1f) {
            dir = Vector2.Normalize(dir);
        }

        world.TrySetInput(dimensionName, entityId, dir.X, dir.Y);
    }
}