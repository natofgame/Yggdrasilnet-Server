using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Systems;

/// <summary>
///     The system pipeline.
/// </summary>
public sealed class SystemPipeline {
    private static readonly SystemPhase[] PhaseOrder = [
        SystemPhase.Input,
        SystemPhase.Ai,
        SystemPhase.Physics,
        SystemPhase.Combat,
        SystemPhase.Cleanup
    ];

    private readonly HashSet<string> _systemIds = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<SystemPhase, List<ISystem>> _systemsByPhase = new() {
        [SystemPhase.Input] = [],
        [SystemPhase.Ai] = [],
        [SystemPhase.Physics] = [],
        [SystemPhase.Combat] = [],
        [SystemPhase.Cleanup] = []
    };

    /// <summary>
    ///     Adds a system to the pipeline.
    /// </summary>
    /// <param name="system"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Add(ISystem system) {
        ArgumentNullException.ThrowIfNull(system);

        if (!_systemIds.Add(system.Id)) {
            throw new InvalidOperationException($"System '{system.Id}' is already registered.");
        }

        _systemsByPhase[system.Phase].Add(system);
    }

    /// <summary>
    ///     Updates all systems in the pipeline.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="dt"></param>
    public void Update(Dimension dimension, float dt) {
        foreach (var phase in PhaseOrder) {
            var systems = _systemsByPhase[phase];
            foreach (var system in systems)
                system.Update(dimension, dt);
        }
    }
}