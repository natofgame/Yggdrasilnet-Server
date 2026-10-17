using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Models;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.World.Dimensions;

/// <summary>
///     A dimension: a set of entities simulated together by one system pipeline.
/// </summary>
/// <remarks>
///     Not thread-safe: the world belongs to the game loop thread. Network callbacks, console input and
///     plugins reach it through events drained on that thread, never directly from another thread.
/// </remarks>
public class Dimension : IDimension {
    private readonly Dictionary<int, Entity> _entities = new();

    private readonly Dictionary<Type, HashSet<int>> _entityIndex = new();
    private readonly HashSet<long> _loadedChunks = [];

    private readonly SystemPipeline _systems;

    public Dimension(string id, IWorld world, SystemPipeline systems) {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        World = world ?? throw new ArgumentNullException(nameof(world));
        _systems = systems ?? throw new ArgumentNullException(nameof(systems));
    }

    public string Id { get; }
    public IWorld World { get; }
    public long Tick { get; private set; }

    public ComponentManager ComponentManager { get; } = new();

    public Entity Spawn(string definitionId, Vector3 position) {
        var entity = new Entity(definitionId, position, this);

        _entities[entity.UniqueId] = entity;

        ApplyDefaultBehaviors(entity);

        return entity;
    }

    public bool Despawn(Entity entity) {
        return entity.Dimension == this && Despawn(entity.UniqueId);
    }

    public bool Despawn(int entityId) {
        if (!_entities.Remove(entityId, out var entity)) {
            return false;
        }

        UnindexEntity(entityId);
        entity.ClearComponents();
        return true;
    }

    public bool TryGetEntity(int entityId, [NotNullWhen(true)] out Entity? entity) {
        return _entities.TryGetValue(entityId, out entity);
    }

    public Entity GetEntity(int entityId) {
        return _entities.TryGetValue(entityId, out var entity)
            ? entity
            : throw new KeyNotFoundException($"Entity {entityId} not found in dimension {Id}");
    }

    public IReadOnlyCollection<Entity> GetAllEntities() {
        return _entities.Values.ToList().AsReadOnly();
    }


    // Queries return a snapshot so callers can spawn, despawn or change components while iterating.
    public IEnumerable<Entity> Query<T>() where T : class, IComponent {
        if (!_entityIndex.TryGetValue(typeof(T), out var entityIds)) {
            return [];
        }

        var result = new List<Entity>(entityIds.Count);
        foreach (var id in entityIds)
            if (_entities.TryGetValue(id, out var entity)) {
                result.Add(entity);
            }

        return result;
    }

    public IEnumerable<Entity> Query<T1, T2>()
        where T1 : class, IComponent
        where T2 : class, IComponent {
        if (!_entityIndex.TryGetValue(typeof(T1), out var ids1) ||
            !_entityIndex.TryGetValue(typeof(T2), out var ids2)) {
            return [];
        }

        var (small, large) = ids1.Count <= ids2.Count ? (ids1, ids2) : (ids2, ids1);

        var result = new List<Entity>(small.Count);
        foreach (var id in small)
            if (large.Contains(id) && _entities.TryGetValue(id, out var entity)) {
                result.Add(entity);
            }

        return result;
    }

    public IEnumerable<Entity> Query<T1, T2, T3>()
        where T1 : class, IComponent
        where T2 : class, IComponent
        where T3 : class, IComponent {
        return Query<T1, T2>().Where(entity => entity.Has<T3>()).ToList();
    }

    public bool TrySetInput(int entityId, Vector2 input) {
        if (!TryGetEntity(entityId, out var entity) || !entity.Has<InputComponent>()) {
            return false;
        }

        var inputComponent = entity.Get<InputComponent>();
        inputComponent.Direction = input;
        entity.Set(inputComponent);
        return true;
    }

    public bool LoadChunk(int chunkX, int chunkZ) {
        return _loadedChunks.Add(ToChunkKey(chunkX, chunkZ));
    }

    public bool UnloadChunk(int chunkX, int chunkZ) {
        return _loadedChunks.Remove(ToChunkKey(chunkX, chunkZ));
    }

    public bool IsChunkLoaded(int chunkX, int chunkZ) {
        return _loadedChunks.Contains(ToChunkKey(chunkX, chunkZ));
    }

    public void Update(float deltaTime) {
        Tick++;
        _systems.Update(this, deltaTime);
    }

    private void IndexEntity(int entityId, Type componentType) {
        if (!_entityIndex.TryGetValue(componentType, out var set)) {
            set = [];
            _entityIndex[componentType] = set;
        }

        set.Add(entityId);
    }

    private void UnindexEntity(int entityId, Type componentType) {
        if (_entityIndex.TryGetValue(componentType, out var set)) {
            set.Remove(entityId);
        }
    }

    private void UnindexEntity(int entityId) {
        foreach (var set in _entityIndex.Values)
            set.Remove(entityId);
    }

    private void ApplyDefaultBehaviors(Entity entity) {
        if (!World.Behaviors.TryGetEntity(entity.Id, out var behaviorDefinition)) {
            return;
        }

        foreach (var behavior in behaviorDefinition.Behaviors)
            behavior.Apply(this, entity.UniqueId);
    }

    public bool TryGetSpell(string spellId, out BehaviorDefinition definition) {
        return World.Behaviors.TryGetSpell(spellId, out definition);
    }

    private static long ToChunkKey(int chunkX, int chunkZ) {
        return ((long)chunkX << 32) | (uint)chunkZ;
    }
    
    public T? GetSystem<T>() where T : class, ISystem {
        return _systems.Get<T>();
    }

    internal void OnEntityComponentChanged(Entity entity, Type componentType, object component) {
        IndexEntity(entity.UniqueId, componentType);
    }


    internal void OnEntityComponentRemoved(Entity entity, Type componentType) {
        UnindexEntity(entity.UniqueId, componentType);
    }
}