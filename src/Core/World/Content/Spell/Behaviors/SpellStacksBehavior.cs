using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public enum SpellStackPolicyType {
    None = 0,
    Additive = 1
}

public sealed class SpellStacksBehavior : IBehavior {
    public string Type { get; set; } = "none";
    public int MaxStacks { get; set; }
    public float DurationSeconds { get; set; }
    public bool RefreshDurationOnAdd { get; set; } = true;

    public SpellStackPolicyType ResolvedType => Enum.TryParse<SpellStackPolicyType>(Type, true, out var value)
        ? value
        : throw new InvalidDataException($"Unknown stack policy type '{Type}'.");

    void IBehavior.Apply(Dimension dimension, int entityId, Vector3 spawnPosition) { }

    public void Validate() {
        if (MaxStacks < 0) {
            throw new InvalidDataException("stacks.maxStacks must be >= 0.");
        }

        BehaviorValidation.EnsureNonNegative(DurationSeconds, "stacks.durationSeconds");
        _ = ResolvedType;
    }
}
