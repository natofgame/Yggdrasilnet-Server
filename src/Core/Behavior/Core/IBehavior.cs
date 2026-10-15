using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Behavior.Core;

public interface IBehavior {
    /// <summary>
    ///     Applies the behavior to the entity.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="entityId"></param>
    void Apply(Dimension dimension, int entityId);
}