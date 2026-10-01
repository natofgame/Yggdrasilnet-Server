using Yggdrasilnet.Gameplay.Enums;
using Yggdrasilnet.Network.Enums;
using Yggdrasilnet.Server.Simulation.Content;
using Yggdrasilnet.Server.Simulation.Content.Spell;
using Yggdrasilnet.Server.Simulation.Content.Spell.Definitions;
using Yggdrasilnet.Server.Simulation.Content.Spell.Runtime;
using Yggdrasilnet.Server.Simulation.World.Component;

namespace Yggdrasilnet.Server.Simulation.World.System.Combat;

public sealed class SpellCastIntentSystem(
    DefinitionRegistry<SpellDefinition> spellDefinitions,
    SpellPhaseSystem phaseSystem,
    SpellRuntimePipeline spellRuntime
) : ISystem {
    public void Update(World world, float deltaTime) {
        foreach (var (entity, intent) in world.Query<CastSpellIntentComponent>().ToArray()) {
            entity.RemoveComponent<CastSpellIntentComponent>();
            var hasActive = entity.TryGetComponent<ActionStateComponent>(out var active);
            if (hasActive && active.Phase != SpellPhase.None &&
                !phaseSystem.TryInterruptReturn(world, active)) {
                continue;
            }
            if (!entity.TryGetComponent<SpellbookComponent>(out var book) ||
                intent.SpellIndex >= book.Spells.Count) {
                continue;
            }
            var spellId = book.Spells[intent.SpellIndex];
            if ((!string.IsNullOrEmpty(intent.SpellDefinitionId) && intent.SpellDefinitionId != spellId) ||
                !spellDefinitions.TryGet(spellId, out var spell) ||
                !SpellCastValidator.TryCreate(world, entity, spell, out var context)) {
                continue;
            }
            if (!spellRuntime.TryBeginCast(book, spell)) {
                continue;
            }

            var timing = spell.Timing;
            entity.AddComponent(new ActionStateComponent {
                ActionType = 1,
                Phase = SpellPhase.Anticipation,
                SpellId = spell.Id,
                SpellType = spell.Type,
                CasterEntityId = entity.Id,
                Context = context,
                CurrentPhaseDuration = timing.Anticipation,
                PhaseTimeRemaining = timing.Anticipation
            });
        }
    }
}
