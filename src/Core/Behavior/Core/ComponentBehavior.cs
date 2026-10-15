using System.Numerics;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Behavior.Core;

/// <summary>
///     Applies a component to an entity.
/// </summary>
/// <typeparam name="TComponent"></typeparam>
public abstract class ComponentBehavior<TComponent> : IBehavior
    where TComponent : class, IComponent {
    /// <inheritdoc />
    public void Apply(Dimension dimension, int entityId) {
        if (dimension.TryGetEntity(entityId, out var entity)) {
            entity.Set(GetComponent(entity.Position));
        }
    }

    /// <summary>
    ///     Creates a new component instance.
    /// </summary>
    /// <param name="spawnPosition"></param>
    /// <returns></returns>
    protected abstract TComponent GetComponent(Vector3 spawnPosition);
}