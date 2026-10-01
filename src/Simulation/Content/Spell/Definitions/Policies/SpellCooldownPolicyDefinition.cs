namespace Yggdrasilnet.Server.Simulation.Content.Spell.Definitions.Policies;

public enum SpellCooldownPolicyType {
    None = 0,
    Fixed = 1,
}

public sealed class SpellCooldownPolicyDefinition {
    public SpellCooldownPolicyType Type { get; set; } = SpellCooldownPolicyType.None;
    public float DurationSeconds { get; set; }
}
