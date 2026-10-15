using Yggdrasilnet.Server.Core.World.Entities;
using GameWorld = Yggdrasilnet.Server.Core.World.Dimensions.Dimension;

namespace Yggdrasilnet.Server.Core.Events;

public abstract class EntityEvent : WorldEvent {
    protected EntityEvent(int entityId) {
        if (entityId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity id must be greater than zero.");
        }

        EntityId = entityId;
    }

    public int EntityId { get; }
    public Entity Entity { get; private set; } = null!;
    public GameWorld Dimension { get; private set; } = null!;

    internal bool TryResolve() {
        if (!Context.TryResolveEntity(EntityId, out var entity) || entity is null) {
            return false;
        }

        Entity = entity;
        Dimension = (GameWorld)entity.Dimension;
        return true;
    }
}