using System.Numerics;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Entities;

public interface IEntity {
    int UniqueId { get; }

    string Id { get; }

    Vector3 Position { get; set; }

    Vector3 PreviousPosition { get; set; }

    IDimension Dimension { get; }

    IWorld World { get; }

    T? GetOrDefault<T>() where T : class, IComponent;

    bool Has<T>() where T : class, IComponent;

    T Set<T>(T component) where T : class, IComponent;

    bool Remove<T>() where T : class, IComponent;

    Player ToPlayer();
}