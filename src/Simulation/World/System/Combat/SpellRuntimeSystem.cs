using Yggdrasilnet.Server.Simulation.Content.Spell.Runtime;
using Yggdrasilnet.Server.Simulation.World.Component;

namespace Yggdrasilnet.Server.Simulation.World.System.Combat;

public sealed class SpellRuntimeSystem(SpellRuntimePipeline spellRuntime) : ISystem {
    public void Update(World world, float deltaTime) {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0f) {
            return;
        }

        foreach (var (_, book) in world.Query<SpellbookComponent>()) {
            spellRuntime.Tick(book, deltaTime);
        }
    }
}
