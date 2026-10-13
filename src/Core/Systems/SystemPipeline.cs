using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Systems;

public sealed class SystemPipeline {
    private static readonly WorldSystemPhase[] PhaseOrder = [
        WorldSystemPhase.Input,
        WorldSystemPhase.Ai,
        WorldSystemPhase.Physics,
        WorldSystemPhase.Combat,
        WorldSystemPhase.Cleanup
    ];

    private readonly HashSet<string> _systemIds = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<WorldSystemPhase, List<IWorldSystem>> _systemsByPhase = new() {
        [WorldSystemPhase.Input] = [],
        [WorldSystemPhase.Ai] = [],
        [WorldSystemPhase.Physics] = [],
        [WorldSystemPhase.Combat] = [],
        [WorldSystemPhase.Cleanup] = []
    };

    public void Add(IWorldSystem system) {
        ArgumentNullException.ThrowIfNull(system);

        if (!_systemIds.Add(system.Id)) {
            throw new InvalidOperationException($"System '{system.Id}' is already registered.");
        }

        _systemsByPhase[system.Phase].Add(system);
    }

    public void Update(Dimension dimension, float dt) {
        foreach (var phase in PhaseOrder) {
            var systems = _systemsByPhase[phase];
            for (var i = 0; i < systems.Count; i++)
                systems[i].Update(dimension, dt);
        }
    }
}
