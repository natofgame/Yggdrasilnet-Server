namespace Yggdrasilnet.Server.Simulation.Content.Spell.Definitions.Policies;

public enum SpellStackPolicyType {
    None = 0,
    Additive = 1,
}

public sealed class SpellStackPolicyDefinition {
    public SpellStackPolicyType Type { get; set; } = SpellStackPolicyType.None;
    public int MaxStacks { get; set; }
    public float DurationSeconds { get; set; }
    public bool RefreshDurationOnAdd { get; set; } = true;
}
