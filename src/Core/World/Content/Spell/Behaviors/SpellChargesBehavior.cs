using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public enum SpellChargePolicyType {
    None = 0,
    Regenerating = 1
}

public sealed class SpellChargesBehavior : IBehavior {
    public string Type { get; set; } = "none";
    public int MaxCharges { get; set; }
    public float RecoverySeconds { get; set; }
    public bool StartFull { get; set; } = true;

    public SpellChargePolicyType ResolvedType => Enum.TryParse<SpellChargePolicyType>(Type, true, out var value)
        ? value
        : throw new InvalidDataException($"Unknown charge policy type '{Type}'.");

    void IBehavior.Apply(Dimension dimension, int entityId) { }

    public void Validate() {
        if (MaxCharges < 0) {
            throw new InvalidDataException("charges.maxCharges must be >= 0.");
        }

        BehaviorValidation.EnsureNonNegative(RecoverySeconds, "charges.recoverySeconds");
        _ = ResolvedType;
    }
}