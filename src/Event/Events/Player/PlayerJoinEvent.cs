using Yggdrasilnet.Server.Event.Types;

namespace Yggdrasilnet.Server.Event.Events.Player;

/// <summary>
///  Event triggered when a player joins.
/// </summary>
public class PlayerJoinEvent : PlayerEvent {
    public PlayerJoinEvent(Core.World.Entities.Player player) : base(player) { }
}