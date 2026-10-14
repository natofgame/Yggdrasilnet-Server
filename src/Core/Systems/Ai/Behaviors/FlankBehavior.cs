using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;

/// <summary>
///  Approaches the target, then flanks it.
/// </summary>
public sealed class FlankBehavior : AiBehavior {
    public float Speed { get; set; } = 3.2f;
    public float Distance { get; set; } = 2.5f;
    
    /// <inheritdoc/>
    public override AiCombatState State => AiCombatState.Approach;
    
    /// <inheritdoc/>
    public override float TransitionLockDuration => 0.45f;
    
    /// <inheritdoc/>
    public override float Evaluate(in AiContext context, in BrainComponent ai) {
        if (!context.HasTarget) {
            return 0f;
        }

        var distanceFactor = Math.Clamp(
            (context.TargetDistance - Distance) / MathF.Max(0.1f, Distance * 2f),
            0f,
            1f);

        if (context.HasAttackToken && context.AttackCooldown <= 0f) {
            return 0.45f + context.Curiosity01 * 0.25f + distanceFactor * 0.25f;
        }

        if (!context.HasAttackToken && context.TargetDistance > Distance * 1.4f) {
            return 0.2f + context.Curiosity01 * 0.15f;
        }

        return 0f;
    }
    
    /// <inheritdoc/>
    public override AiIntent BuildIntent(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt
    ) {
        if (!context.HasDirection) {
            return AiIntent.Stop();
        }

        var toTarget = context.TargetDirection;
        var orbitDirection = ai.HoldOrbitDirection is 1f or -1f
            ? ai.HoldOrbitDirection
            : entity.UniqueId % 2 == 0
                ? 1f
                : -1f;
        var tangent = new Vector2(-toTarget.Y, toTarget.X) * orbitDirection;

        var radialError = context.TargetDistance - MathF.Max(0.1f, Distance);
        var radialStrength = Math.Clamp(radialError / MathF.Max(0.1f, Distance), -0.85f, 0.85f);
        var move = context.TargetDistance > Distance * 4f
            ? toTarget
            : tangent + toTarget * radialStrength;
        if (move.LengthSquared() > 1f) {
            move = Vector2.Normalize(move);
        }

        return AiIntent.Move(
            move,
            Speed * (0.85f + Math.Clamp(MathF.Abs(radialStrength), 0f, 1f) * 0.3f),
            circleRadius: Distance,
            circleDirection: orbitDirection,
            circleWeight: 0.35f,
            wanderWeight: 0.8f,
            wanderJitter: 0.9f);
    }
}
