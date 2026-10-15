using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public sealed class SpellMetaBehavior : IBehavior {
    public string Type { get; set; } = "projectile";
    public bool SkipReturnPhase { get; set; }
    public float CastMoveSpeedReductionPercent { get; set; }

    public SpellType ResolvedType => Enum.TryParse<SpellType>(Type, true, out var value)
        ? value
        : throw new InvalidDataException($"Unknown spell type '{Type}'.");

    void IBehavior.Apply(Dimension dimension, int entityId) { }

    public void Validate() {
        BehaviorValidation.EnsureNonNegative(CastMoveSpeedReductionPercent, "meta.castMoveSpeedReductionPercent");
        _ = ResolvedType;
    }
}