using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Event.Types;

namespace Yggdrasilnet.Server.Event.Events.Player;

/// <summary>
/// Event triggered when a player leaves the game.
/// </summary>
public class PlayerLeaveEvent : PlayerEvent {
    public PlayerLeaveEvent(Core.World.Entities.Player player) : base(player) { }
}
