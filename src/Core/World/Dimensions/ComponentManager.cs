using System.Collections.Concurrent;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Dimensions;

/// <summary>
///     Gère le stockage et l'accès aux composants des entités dans une dimension.
/// </summary>
public sealed class ComponentManager {
    private readonly ConcurrentDictionary<Type, IComponentStore> _stores = new();

    /// <summary>
    ///     Récupère le magasin de composants pour un type spécifique.
    /// </summary>
    /// <typeparam name="T">Le type de composant.</typeparam>
    /// <returns>Le magasin de composants pour le type spécifié.</returns>
    public ComponentStore<T> GetStore<T>() where T : class, IComponent {
        return (ComponentStore<T>)_stores.GetOrAdd(typeof(T), _ => new ComponentStore<T>());
    }

    /// <summary>
    ///     Récupère tous les magasins de composants existants.
    /// </summary>
    /// <returns>Une collection de tous les magasins de composants.</returns>
    public IEnumerable<IComponentStore> GetAllStores() {
        return _stores.Values;
    }
}

/// <summary>
///     Interface de base pour un magasin de composants.
/// </summary>
public interface IComponentStore {
    /// <summary>
    ///     Vérifie si une entité possède un composant dans ce magasin.
    /// </summary>
    bool Has(int entityId);

    /// <summary>
    ///     Supprime le composant associé à une entité.
    /// </summary>
    bool Remove(int entityId);
}

/// <summary>
///     Magasin de composants typé pour une performance accrue.
/// </summary>
/// <typeparam name="T">Le type de composant stocké.</typeparam>
public sealed class ComponentStore<T> : IComponentStore where T : class, IComponent {
    private readonly ConcurrentDictionary<int, T> _components = new();

    /// <inheritdoc />
    public bool Has(int entityId) {
        return _components.ContainsKey(entityId);
    }

    /// <inheritdoc />
    public bool Remove(int entityId) {
        return _components.TryRemove(entityId, out _);
    }

    /// <summary>
    ///     Récupère le composant d'une entité. Lance une exception si non trouvé.
    /// </summary>
    public T Get(int entityId) {
        return _components[entityId];
    }

    /// <summary>
    ///     Tente de récupérer le composant d'une entité.
    /// </summary>
    public bool TryGet(int entityId, out T? component) {
        return _components.TryGetValue(entityId, out component);
    }

    /// <summary>
    ///     Définit le composant pour une entité.
    /// </summary>
    public void Set(int entityId, T component) {
        _components[entityId] = component;
    }

    /// <summary>
    ///     Récupère tous les composants de ce type.
    /// </summary>
    public IEnumerable<T> GetAll() {
        return _components.Values;
    }
}
