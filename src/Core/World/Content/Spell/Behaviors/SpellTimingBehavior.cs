using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public sealed class SpellTimingBehavior : IBehavior {
    public float Anticipation { get; set; }
    public float Strike { get; set; }
    public float Impact { get; set; }
    public float Return { get; set; }

    void IBehavior.Apply(Dimension dimension, int entityId) { }

    public void Validate() {
        BehaviorValidation.EnsureNonNegative(
            (Anticipation, "timing.anticipation"),
            (Strike, "timing.strike"),
            (Impact, "timing.impact"),
            (Return, "timing.return"));
    }
}