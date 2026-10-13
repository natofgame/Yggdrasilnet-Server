using System.Text.Json.Serialization;
using Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WanderBehavior), "wander")]
[JsonDerivedType(typeof(AttackBehavior), "attack")]
[JsonDerivedType(typeof(HoldBehavior), "hold")]
[JsonDerivedType(typeof(RepositionBehavior), "reposition")]
[JsonDerivedType(typeof(FleeBehavior), "flee")]
[JsonDerivedType(typeof(FlankBehavior), "flank")]
public abstract class AiBehavior {
    public abstract AiCombatState State { get; }
    public virtual float TransitionLockDuration => 0.4f;
    public abstract float Evaluate(in AiContext context, in BrainComponent ai);

    public abstract AiIntent BuildIntent(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt);
}
