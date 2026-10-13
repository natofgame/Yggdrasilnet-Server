using System.Numerics;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.Behavior.Core;

public abstract class ComponentBehavior<TComponent> : IBehavior
    where TComponent : class, IComponent {

    public void Apply(Dimension dimension, int entityId, Vector3 spawnPosition) {
        if (dimension.TryGetEntity(entityId, out var entity)) {
            Apply(entity);
        }
    }

    protected abstract TComponent GetComponent(Vector3 spawnPosition);

    public void Apply(Entity entity) {
        entity.Set(GetComponent(entity.Position));
    }
}
