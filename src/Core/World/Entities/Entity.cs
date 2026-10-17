using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.World.Entities;

public class Entity : IEntity {
    private static int _nextId;

    public Entity(string id, Vector3 position, IDimension? dimension = null, int uniqueId = -1) {
        if (string.IsNullOrWhiteSpace(id)) {
            throw new ArgumentException("Definition ID cannot be null or empty", nameof(id));
        }

        UniqueId = uniqueId == -1 ? Interlocked.Increment(ref _nextId) : uniqueId;
        
        Id = id;
        Position = position;
        PreviousPosition = position;

        if (dimension is not null) {
            Dimension = dimension;
        }
    }

    public int UniqueId { get; }

    public string Id { get; }

    public Vector3 Position { get; set; }

    public Vector3 PreviousPosition { get; set; }

    public IDimension Dimension { get; } = null!;

    public IWorld World => Dimension.World;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetOrDefault<T>() where T : class, IComponent {
        var store = GetComponentManager().GetStore<T>();
        return store.TryGet(UniqueId, out var component) ? component : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : class, IComponent {
        var store = GetComponentManager().GetStore<T>();
        return store.Has(UniqueId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Set<T>(T component) where T : class, IComponent {
        ArgumentNullException.ThrowIfNull(component);

        var store = GetComponentManager().GetStore<T>();
        store.Set(UniqueId, component);

        ((Dimension)Dimension).OnEntityComponentChanged(this, typeof(T), component);
        return component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove<T>() where T : class, IComponent {
        var store = GetComponentManager().GetStore<T>();
        var removed = store.Remove(UniqueId);

        if (removed) {
            ((Dimension)Dimension).OnEntityComponentRemoved(this, typeof(T));
        }

        return removed;
    }

    /// <inheritdoc />
    public void Teleport(Vector3 position) {
        Position = position;
        PreviousPosition = position;

        if (TryGet<MovementComponent>(out var movement)) {
            movement.Velocity = Vector3.Zero;
        }
    }

    private ComponentManager GetComponentManager() {
        return ((Dimension)Dimension).ComponentManager;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>() where T : class, IComponent {
        var store = GetComponentManager().GetStore<T>();
        return store.Get(UniqueId);
    }

    public bool TryGet<T>([NotNullWhen(true)] out T? component) where T : class, IComponent {
        var store = GetComponentManager().GetStore<T>();
        return store.TryGet(UniqueId, out component);
    }

    public void ClearComponents() {
        var manager = GetComponentManager();
        foreach (var storeObj in manager.GetAllStores())
            storeObj.Remove(UniqueId);
    }

    public Player ToPlayer() => new(this);

    public override string ToString() {
        return $"Entity#{UniqueId} [{Id}] @ {Position}";
    }
}