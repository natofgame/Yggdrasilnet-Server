using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.World;

public sealed class World : IWorld {
    private readonly ConcurrentDictionary<string, Dimension> _dimensions = new(StringComparer.OrdinalIgnoreCase);
    private Dimension? _main;

    public World(Simulation simulation, BehaviorPackRegistry? behaviors = null) {
        Behaviors = behaviors ?? new BehaviorPackRegistry();
        Simulation = simulation;
    }

    public Simulation Simulation { get; }
    public long Tick { get; private set; }
    public BehaviorPackRegistry Behaviors { get; }
    public IDimension MainDimension => _main ?? throw new InvalidOperationException("No main dimension set");
    public IReadOnlyCollection<IDimension> Dimensions => _dimensions.Values.ToList().AsReadOnly();

    public Dimension CreateDimension(string id, SystemPipeline? systems = null, bool isMain = false) {
        if (string.IsNullOrWhiteSpace(id)) {
            throw new ArgumentException("Dimension ID cannot be null or empty", nameof(id));
        }

        if (_dimensions.ContainsKey(id)) {
            throw new InvalidOperationException($"Dimension '{id}' already exists");
        }

        var dimension = new Dimension(id, this, systems ?? new SystemPipeline());
        _dimensions[id] = dimension;

        if (isMain || _main == null) {
            _main = dimension;
        }

        return dimension;
    }

    public bool TryGetDimension(string id, [NotNullWhen(true)] out IDimension? dimension) {
        if (_dimensions.TryGetValue(id, out var dim)) {
            dimension = dim;
            return true;
        }

        dimension = null;
        return false;
    }

    public IDimension GetDimension(string id) {
        return _dimensions.TryGetValue(id, out var dimension)
            ? dimension
            : throw new KeyNotFoundException($"Dimension '{id}' not found");
    }

    public Entity Spawn(string definitionId, Vector3 position) {
        return MainDimension.Spawn(definitionId, position);
    }

    public Entity Spawn(string definitionId, string dimensionId, Vector3 position) {
        var dimension = GetDimension(dimensionId);
        return dimension.Spawn(definitionId, position);
    }

    public bool Despawn(Entity entity) {
        return ((Dimension)entity.Dimension).Despawn(entity);
    }

    public bool Despawn(int entityId) {
        foreach (var dimension in _dimensions.Values)
            if (dimension.TryGetEntity(entityId, out var entity)) {
                return dimension.Despawn(entity);
            }

        return false;
    }

    public bool TryGetEntity(int entityId, [NotNullWhen(true)] out Entity? entity) {
        foreach (var dimension in _dimensions.Values)
            if (dimension.TryGetEntity(entityId, out entity)) {
                return true;
            }

        entity = null;
        return false;
    }

    public bool TryResolveEntity(int entityId, out Entity? entity) {
        foreach (var dimension in _dimensions.Values)
            if (dimension.TryGetEntity(entityId, out entity)) {
                return true;
            }

        entity = null;
        return false;
    }

    public bool TrySetInput(string dimensionName, int entityId, float x, float z) {
        return TryGetDimension(dimensionName, out var dimension) && dimension.TrySetInput(entityId, new Vector2(x, z));
    }

    public bool TryGetEntity(string dimensionName, int entityId, out Entity? entity) {
        if (TryGetDimension(dimensionName, out var dimension)) {
            return dimension.TryGetEntity(entityId, out entity);
        }

        entity = null;
        return false;
    }

    public bool TryDespawn(string dimensionName, int entityId) {
        return TryGetDimension(dimensionName, out var dimension) && dimension.Despawn(entityId);
    }

    public IEnumerable<Entity> Query<T>() where T : class, IComponent {
        return _dimensions.Values.SelectMany(dimension => dimension.Query<T>());
    }

    public IEnumerable<Entity> Query<T1, T2>()
        where T1 : class, IComponent
        where T2 : class, IComponent {
        return _dimensions.Values.SelectMany(dimension => dimension.Query<T1, T2>());
    }

    public IEnumerable<Entity> Query<T1, T2, T3>() where T1 : class, IComponent
        where T2 : class, IComponent
        where T3 : class, IComponent {
        return _dimensions.Values.SelectMany(dimension => dimension.Query<T1, T2, T3>());
    }

    public void Update(float deltaTime) {
        Tick++;

        foreach (var dimension in _dimensions.Values)
            dimension.Update(deltaTime);
    }
}