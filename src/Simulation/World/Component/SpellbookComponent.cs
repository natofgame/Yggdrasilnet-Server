namespace Yggdrasilnet.Server.Simulation.World.Component;

public sealed class SpellRuntimeState {
    public float CooldownRemainingSeconds { get; set; }
    public int ChargesCurrent { get; set; }
    public float ChargeRecoveryRemainingSeconds { get; set; }
    public int StackCount { get; set; }
    public float StackRemainingSeconds { get; set; }
    public int Version { get; set; }
}

public sealed class SpellbookRuntimeState {
    public Dictionary<string, SpellRuntimeState> Spells { get; } = [];
}

public sealed class SpellbookComponent : IComponent {
    public List<string> Spells { get; set; } = [];
    public SpellbookRuntimeState Runtime { get; } = new();
}
