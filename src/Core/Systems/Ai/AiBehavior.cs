using System.Text.Json.Serialization;
using Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

/// <summary>
///     Represents an AI behavior.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WanderBehavior), "wander")]
[JsonDerivedType(typeof(AttackBehavior), "attack")]
[JsonDerivedType(typeof(HoldBehavior), "hold")]
[JsonDerivedType(typeof(RepositionBehavior), "reposition")]
[JsonDerivedType(typeof(FleeBehavior), "flee")]
[JsonDerivedType(typeof(FlankBehavior), "flank")]
public abstract class AiBehavior {
    /// <summary>
    ///     The AI's current state.'
    /// </summary>
    public abstract AiCombatState State { get; }

    /// <summary>
    ///     The duration of the AI's transition to the current state.'
    /// </summary>
    public virtual float TransitionLockDuration => 0.4f;

    /// <summary>
    ///     Evaluates the AI's intent.'
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ai"></param>
    /// <returns></returns>
    public abstract float Evaluate(in AiContext context, in BrainComponent ai);

    /// <summary>
    ///     Builds the AI's intent.'
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="entity"></param>
    /// <param name="ai"></param>
    /// <param name="context"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    public abstract AiIntent BuildIntent(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt);
}