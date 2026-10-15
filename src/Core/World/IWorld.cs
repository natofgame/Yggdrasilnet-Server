using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.World;

public interface IWorld {
    Simulation Simulation { get; }
    long Tick { get; }
    BehaviorPackRegistry Behaviors { get; }
    IDimension MainDimension { get; }
    IReadOnlyCollection<IDimension> Dimensions { get; }

    Dimension CreateDimension(string id, SystemPipeline? systems = null, bool isMain = false);
    bool TryGetDimension(string id, [NotNullWhen(true)] out IDimension? dimension);
    IDimension GetDimension(string id);

    Entity Spawn(string definitionId, Vector3 position);
    Entity Spawn(string definitionId, string dimensionId, Vector3 position);
    bool Despawn(Entity entity);
    bool Despawn(int entityId);
    bool TryGetEntity(int entityId, out Entity? entity);
    bool TryResolveEntity(int entityId, out Entity? entity);
    bool TrySetInput(string dimensionName, int entityId, float x, float z);
    bool TryGetEntity(string dimensionName, int entityId, out Entity? entity);
    bool TryDespawn(string dimensionName, int entityId);

    IEnumerable<Entity> Query<T>() where T : class, IComponent;
    IEnumerable<Entity> Query<T1, T2>() where T1 : class, IComponent where T2 : class, IComponent;

    IEnumerable<Entity> Query<T1, T2, T3>() where T1 : class, IComponent
        where T2 : class, IComponent
        where T3 : class, IComponent;

    void Update(float deltaTime);
}