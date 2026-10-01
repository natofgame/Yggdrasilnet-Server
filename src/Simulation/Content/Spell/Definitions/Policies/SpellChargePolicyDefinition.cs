namespace Yggdrasilnet.Server.Simulation.Content.Spell.Definitions.Policies;

public enum SpellChargePolicyType {
    None = 0,
    Regenerating = 1,
}

public sealed class SpellChargePolicyDefinition {
    public SpellChargePolicyType Type { get; set; } = SpellChargePolicyType.None;
    public int MaxCharges { get; set; }
    public float RecoverySeconds { get; set; }
    public bool StartFull { get; set; } = true;
}
