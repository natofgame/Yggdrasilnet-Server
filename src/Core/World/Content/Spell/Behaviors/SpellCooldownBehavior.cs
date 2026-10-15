using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public enum SpellCooldownPolicyType {
    None = 0,
    Fixed = 1
}

public sealed class SpellCooldownBehavior : IBehavior {
    public string Type { get; set; } = "none";
    public float DurationSeconds { get; set; }

    public SpellCooldownPolicyType ResolvedType => Enum.TryParse<SpellCooldownPolicyType>(Type, true, out var value)
        ? value
        : throw new InvalidDataException($"Unknown cooldown policy type '{Type}'.");

    void IBehavior.Apply(Dimension dimension, int entityId) { }

    public void Validate() {
        BehaviorValidation.EnsureNonNegative(DurationSeconds, "cooldown.durationSeconds");
        _ = ResolvedType;
    }
}