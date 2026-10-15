using Yggdrasilnet.Server.Core;

namespace Yggdrasilnet.Server.Event.Types;

/// <summary>
/// Event triggered by the simulation.
/// </summary>
public class SimulationEvent : ServerEvent {
    /// <summary>
    ///  The simulation that the event occurred in.
    /// </summary>
    public Simulation Simulation { get; private set; }
    
    protected SimulationEvent(Simulation simulation) : base(simulation.Server) {
        Simulation = simulation;
    }
}