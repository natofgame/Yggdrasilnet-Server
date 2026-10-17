using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Dimensions;

/// <summary>
///     Stores the components of the entities of a dimension.
/// </summary>
/// <remarks>
///     Not thread-safe: like the rest of the world, it must only be used from the game loop thread.
/// </remarks>
public sealed class ComponentManager {
    private readonly Dictionary<Type, IComponentStore> _stores = new();

    /// <summary>
    ///     Gets the store for a component type, creating it on first use.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The store for <typeparamref name="T" />.</returns>
    public ComponentStore<T> GetStore<T>() where T : class, IComponent {
        if (!_stores.TryGetValue(typeof(T), out var store)) {
            store = new ComponentStore<T>();
            _stores[typeof(T)] = store;
        }

        return (ComponentStore<T>)store;
    }

    /// <summary>
    ///     Gets all existing component stores.
    /// </summary>
    /// <returns>Every store created so far.</returns>
    public IEnumerable<IComponentStore> GetAllStores() {
        return _stores.Values;
    }
}

/// <summary>
///     Base interface of a component store.
/// </summary>
public interface IComponentStore {
    /// <summary>
    ///     Checks whether an entity has a component in this store.
    /// </summary>
    bool Has(int entityId);

    /// <summary>
    ///     Removes the component of an entity.
    /// </summary>
    bool Remove(int entityId);
}

/// <summary>
///     Typed component store.
/// </summary>
/// <typeparam name="T">The stored component type.</typeparam>
public sealed class ComponentStore<T> : IComponentStore where T : class, IComponent {
    private readonly Dictionary<int, T> _components = new();

    /// <inheritdoc />
    public bool Has(int entityId) {
        return _components.ContainsKey(entityId);
    }

    /// <inheritdoc />
    public bool Remove(int entityId) {
        return _components.Remove(entityId);
    }

    /// <summary>
    ///     Gets the component of an entity. Throws if the entity has none.
    /// </summary>
    public T Get(int entityId) {
        return _components[entityId];
    }

    /// <summary>
    ///     Tries to get the component of an entity.
    /// </summary>
    public bool TryGet(int entityId, out T? component) {
        return _components.TryGetValue(entityId, out component);
    }

    /// <summary>
    ///     Sets the component of an entity.
    /// </summary>
    public void Set(int entityId, T component) {
        _components[entityId] = component;
    }

    /// <summary>
    ///     Gets all components of this type.
    /// </summary>
    public IEnumerable<T> GetAll() {
        return _components.Values;
    }
}
