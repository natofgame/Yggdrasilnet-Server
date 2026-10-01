using Yggdrasilnet.Server.Simulation.Content;
using Yggdrasilnet.Server.Simulation.Content.Spell.Definitions;
using Yggdrasilnet.Server.Simulation.Content.Spell.Definitions.Policies;
using Yggdrasilnet.Server.Simulation.World.Component;

namespace Yggdrasilnet.Server.Simulation.Content.Spell.Runtime;

public sealed class SpellRuntimePipeline(DefinitionRegistry<SpellDefinition> spellDefinitions) {
    public bool TryBeginCast(SpellbookComponent book, SpellDefinition spell) {
        var state = GetOrCreateState(book, spell);
        if (!CanCast(spell, state)) {
            return false;
        }

        ConsumeCharges(spell, state);
        StartCooldown(spell, state);
        ApplyCastStacks(spell, state);
        return true;
    }

    public void Tick(SpellbookComponent book, float deltaTime) {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0f) {
            return;
        }

        foreach (var spellId in book.Spells) {
            if (!spellDefinitions.TryGet(spellId, out var definition)) {
                continue;
            }

            var state = GetOrCreateState(book, definition);
            TickCooldown(state, deltaTime);
            TickCharges(definition, state, deltaTime);
            TickStacks(definition, state, deltaTime);
        }
    }

    private static bool CanCast(SpellDefinition spell, SpellRuntimeState state) {
        if (state.CooldownRemainingSeconds > 0f) {
            return false;
        }

        if (spell.Charges.Type == SpellChargePolicyType.Regenerating
            && spell.Charges.MaxCharges > 0
            && state.ChargesCurrent <= 0) {
            return false;
        }

        return true;
    }

    private static void StartCooldown(SpellDefinition spell, SpellRuntimeState state) {
        if (spell.Cooldown.Type != SpellCooldownPolicyType.Fixed || spell.Cooldown.DurationSeconds <= 0f) {
            return;
        }

        state.CooldownRemainingSeconds = spell.Cooldown.DurationSeconds;
        state.Version++;
    }

    private static void ConsumeCharges(SpellDefinition spell, SpellRuntimeState state) {
        if (spell.Charges.Type != SpellChargePolicyType.Regenerating || spell.Charges.MaxCharges <= 0) {
            return;
        }

        state.ChargesCurrent = Math.Max(0, state.ChargesCurrent - 1);
        if (state.ChargesCurrent < spell.Charges.MaxCharges && state.ChargeRecoveryRemainingSeconds <= 0f) {
            state.ChargeRecoveryRemainingSeconds = spell.Charges.RecoverySeconds;
        }
        state.Version++;
    }

    private static void ApplyCastStacks(SpellDefinition spell, SpellRuntimeState state) {
        if (spell.Stacks.Type != SpellStackPolicyType.Additive || spell.Stacks.MaxStacks <= 0) {
            return;
        }

        var next = Math.Min(spell.Stacks.MaxStacks, state.StackCount + 1);
        if (next != state.StackCount) {
            state.StackCount = next;
            state.Version++;
        }

        if (spell.Stacks.DurationSeconds > 0f && (spell.Stacks.RefreshDurationOnAdd || state.StackRemainingSeconds <= 0f)) {
            state.StackRemainingSeconds = spell.Stacks.DurationSeconds;
        }
    }

    private static void TickCooldown(SpellRuntimeState state, float deltaTime) {
        if (state.CooldownRemainingSeconds <= 0f) {
            return;
        }

        var next = Math.Max(0f, state.CooldownRemainingSeconds - deltaTime);
        if (next != state.CooldownRemainingSeconds) {
            state.CooldownRemainingSeconds = next;
            if (next == 0f) {
                state.Version++;
            }
        }
    }

    private static void TickCharges(SpellDefinition spell, SpellRuntimeState state, float deltaTime) {
        if (spell.Charges.Type != SpellChargePolicyType.Regenerating || spell.Charges.MaxCharges <= 0) {
            return;
        }

        if (state.ChargesCurrent >= spell.Charges.MaxCharges || spell.Charges.RecoverySeconds <= 0f) {
            state.ChargeRecoveryRemainingSeconds = 0f;
            return;
        }

        state.ChargeRecoveryRemainingSeconds -= deltaTime;
        while (state.ChargeRecoveryRemainingSeconds <= 0f && state.ChargesCurrent < spell.Charges.MaxCharges) {
            state.ChargesCurrent++;
            state.Version++;
            if (state.ChargesCurrent >= spell.Charges.MaxCharges) {
                state.ChargeRecoveryRemainingSeconds = 0f;
                break;
            }

            state.ChargeRecoveryRemainingSeconds += spell.Charges.RecoverySeconds;
        }
    }

    private static void TickStacks(SpellDefinition spell, SpellRuntimeState state, float deltaTime) {
        if (state.StackCount <= 0 || spell.Stacks.DurationSeconds <= 0f) {
            return;
        }

        state.StackRemainingSeconds = Math.Max(0f, state.StackRemainingSeconds - deltaTime);
        if (state.StackRemainingSeconds > 0f) {
            return;
        }

        state.StackCount = 0;
        state.Version++;
    }

    private static SpellRuntimeState GetOrCreateState(SpellbookComponent book, SpellDefinition spell) {
        if (book.Runtime.Spells.TryGetValue(spell.Id, out var state)) {
            return state;
        }

        state = new SpellRuntimeState {
            ChargesCurrent = ResolveInitialCharges(spell)
        };
        book.Runtime.Spells.Add(spell.Id, state);
        return state;
    }

    private static int ResolveInitialCharges(SpellDefinition spell) {
        if (spell.Charges.Type != SpellChargePolicyType.Regenerating || spell.Charges.MaxCharges <= 0) {
            return 0;
        }

        return spell.Charges.StartFull ? spell.Charges.MaxCharges : 0;
    }
}
