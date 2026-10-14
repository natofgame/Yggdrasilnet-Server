using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Systems;

public interface ISystem {
    /// <summary>
    ///  The unique identifier of the system.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    ///  The phase the system is in.
    /// </summary>
    SystemPhase Phase { get; }
    
    /// <summary>
    ///  Updates the system.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="dt"></param>
    void Update(Dimension dimension, float dt);
}
