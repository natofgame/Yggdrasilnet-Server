using Yggdrasilnet.Server.Core.World;

namespace Yggdrasilnet.Server.Event.Types;

/// <summary>
///  Event triggered when a world event occurs.
/// </summary>
public class WorldEvent : SimulationEvent {
    /// <summary>
    ///  The world that the event occurred in.
    /// </summary>
    public IWorld World { get; set; }
    
    protected WorldEvent(IWorld world) : base(world.Simulation) {
        World = world;
    }
}