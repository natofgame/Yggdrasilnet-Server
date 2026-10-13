using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.World.Dimensions;

public interface IDimension {
    string Id { get; }
    IWorld World { get; }
    long Tick { get; }

    ComponentManager ComponentManager { get; }

    Entity Spawn(string definitionId, Vector3 position);
    bool Despawn(Entity entity);
    bool Despawn(int entityId);
    bool TryGetEntity(int entityId, [NotNullWhen(true)] out Entity? entity);
    Entity GetEntity(int entityId);
    IReadOnlyCollection<Entity> GetAllEntities();

    IEnumerable<Entity> Query<T>() where T : class, IComponent;

    IEnumerable<Entity> Query<T1, T2>()
        where T1 : class, IComponent
        where T2 : class, IComponent;

    IEnumerable<Entity> Query<T1, T2, T3>()
        where T1 : class, IComponent
        where T2 : class, IComponent
        where T3 : class, IComponent;

    bool TrySetInput(int entityId, Vector2 input);

    bool LoadChunk(int chunkX, int chunkZ);
    bool UnloadChunk(int chunkX, int chunkZ);
    bool IsChunkLoaded(int chunkX, int chunkZ);
}
