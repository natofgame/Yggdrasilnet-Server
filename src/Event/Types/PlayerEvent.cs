namespace Yggdrasilnet.Server.Event.Types;

/// <summary>
///  Event triggered when a player event occurs.
/// </summary>
public class PlayerEvent : EntityEvent {
    /// <summary>
    ///  The player that the event occurred in.
    /// </summary>
    public Core.World.Entities.Player Player { get; private set; }
    
    protected PlayerEvent(Core.World.Entities.Player player) : base(player) {
        Player = player;
    }
}