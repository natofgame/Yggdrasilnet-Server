using System.Numerics;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Entities;

public interface IEntity {
    int UniqueId { get; }

    string Id { get; }

    Vector3 Position { get; set; }

    Vector3 PreviousPosition { get; set; }

    /// <summary>
    ///  The dimension the entity is in.
    /// </summary>
    IDimension Dimension { get; }

    /// <summary>
    ///  The world the entity is in.
    /// </summary>
    IWorld World { get; }

    /// <summary>
    ///  Gets the component from the entity.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetOrDefault<T>() where T : class, IComponent;

    /// <summary>
    ///  Checks if the entity has the component.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    bool Has<T>() where T : class, IComponent;

    /// <summary>
    ///  Sets the component on the entity.
    /// </summary>
    /// <param name="component"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Set<T>(T component) where T : class, IComponent;

    /// <summary>
    ///  Removes the component from the entity.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    bool Remove<T>() where T : class, IComponent;

    /// <summary>
    ///     Instantly moves the entity to the given position, in its current dimension.
    ///     No interpolation, sweep or collision between the old and the new position.
    /// </summary>
    /// <param name="position"></param>
    void Teleport(Vector3 position);

    Player ToPlayer();
}