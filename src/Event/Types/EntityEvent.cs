namespace Yggdrasilnet.Server.Event.Types;

/// <summary>
///  Event triggered when an entity event occurs.
/// </summary>
public class EntityEvent : DimensionEvent {
    /// <summary>
    ///  The entity that the event occurred in.
    /// </summary>
    public Core.World.Entities.Entity Entity { get; private set; }
    
    protected EntityEvent(Core.World.Entities.Entity entity) : base(entity.Dimension) {
        Entity = entity;
    }
}