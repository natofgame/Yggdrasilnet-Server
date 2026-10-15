using System.Collections.Concurrent;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Dimensions;

/// <summary>
///     G�re le stockage et l'acc�s aux composants des entit�s dans une dimension.
/// </summary>
public sealed class ComponentManager {
    private readonly ConcurrentDictionary<Type, IComponentStore> _stores = new();

    /// <summary>
    ///     R�cup�re le magasin de composants pour un type sp�cifique.
    /// </summary>
    /// <typeparam name="T">Le type de composant.</typeparam>
    /// <returns>Le magasin de composants pour le type sp�cifi�.</returns>
    public ComponentStore<T> GetStore<T>() where T : class, IComponent {
        return (ComponentStore<T>)_stores.GetOrAdd(typeof(T), _ => new ComponentStore<T>());
    }

    /// <summary>
    ///     R�cup�re tous les magasins de composants existants.
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
    ///     V�rifie si une entit� poss�de un composant dans ce magasin.
    /// </summary>
    bool Has(int entityId);

    /// <summary>
    ///     Supprime le composant associ� � une entit�.
    /// </summary>
    bool Remove(int entityId);
}

/// <summary>
///     Magasin de composants typ� pour une performance accrue.
/// </summary>
/// <typeparam name="T">Le type de composant stock�.</typeparam>
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
    ///     R�cup�re le composant d'une entit�. Lance une exception si non trouv�.
    /// </summary>
    public T Get(int entityId) {
        return _components[entityId];
    }

    /// <summary>
    ///     Tente de r�cup�rer le composant d'une entit�.
    /// </summary>
    public bool TryGet(int entityId, out T? component) {
        return _components.TryGetValue(entityId, out component);
    }

    /// <summary>
    ///     D�finit le composant pour une entit�.
    /// </summary>
    public void Set(int entityId, T component) {
        _components[entityId] = component;
    }

    /// <summary>
    ///     R�cup�re tous les composants de ce type.
    /// </summary>
    public IEnumerable<T> GetAll() {
        return _components.Values;
    }
}