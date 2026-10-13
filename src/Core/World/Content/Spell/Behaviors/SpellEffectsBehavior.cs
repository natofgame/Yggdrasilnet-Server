using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public sealed class SpellEffectEntry {
    public string Type { get; set; } = "damage";
    public string Target { get; set; } = "target";
    public float Amount { get; set; }
    public float Duration { get; set; } = 0.15f;
    public bool ActiveOnAnticipation { get; set; }

    public SpellEffectType ResolvedType => Enum.TryParse<SpellEffectType>(Type, true, out var value)
        ? value
        : throw new InvalidDataException($"Unknown spell effect type '{Type}'.");

    public SpellEffectTarget ResolvedTarget => Enum.TryParse<SpellEffectTarget>(Target, true, out var value)
        ? value
        : throw new InvalidDataException($"Unknown spell effect target '{Target}'.");

    public void Validate() {
        BehaviorValidation.EnsureFinite(Amount, "effects[].amount");
        BehaviorValidation.EnsureNonNegative(Duration, "effects[].duration");
        _ = ResolvedType;
        _ = ResolvedTarget;
    }
}

public sealed class SpellEffectsBehavior : IBehavior {
    public List<SpellEffectEntry> Effects { get; set; } = [];

    void IBehavior.Apply(Dimension dimension, int entityId, Vector3 spawnPosition) { }

    public void Validate() {
        foreach (var effect in Effects)
            effect.Validate();
    }
}
