using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;

public sealed class RepositionBehavior : AiBehavior {
    public float Speed { get; set; } = 4.6f;
    public float BaseRadius { get; set; } = 3.2f;
    public float PrudenceRadius { get; set; } = 2.4f;
    public float AngleOffset { get; set; } = 1.05f;
    public float CircleWeight { get; set; } = 0.5f;
    public float WanderWeight { get; set; } = 0.45f;
    public float WanderJitter { get; set; } = 0.9f;
    public float BurstScore { get; set; } = 0.82f;

    public override AiCombatState State => AiCombatState.Reposition;
    public override float TransitionLockDuration => 0.55f;

    public override float Evaluate(in AiContext context, in BrainComponent ai) {
        if (!context.HasTarget || context.HasAttackToken || ai.RepositionBurstTimer <= 0f) {
            return 0f;
        }

        return Math.Clamp(
            BurstScore
            + context.Curiosity01 * 0.08f
            + context.Impulsivity01 * 0.06f,
            0f,
            1f);
    }

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

        var radius = BaseRadius + context.Prudence01 * PrudenceRadius + ai.HoldRadiusOffset;
        radius = MathF.Max(0.8f, radius);

        var orbitDirection = ai.HoldOrbitDirection is 1f or -1f ? ai.HoldOrbitDirection : 1f;
        var targetPos = entity.Position + ai.TargetDirection * ai.TargetDistance;
        var desiredAngle = ai.SlotAngle + orbitDirection * AngleOffset;
        var desiredSlot = targetPos + new Vector3(MathF.Cos(desiredAngle), 0f, MathF.Sin(desiredAngle)) * radius;

        var toSlot = desiredSlot - entity.Position;
        toSlot.Y = 0f;
        if (toSlot.LengthSquared() <= 0.0001f) {
            return AiIntent.Stop();
        }

        var move = Vector2.Normalize(new Vector2(toSlot.X, toSlot.Z));
        return AiIntent.Move(
            move,
            Speed,
            circleRadius: radius,
            circleDirection: orbitDirection,
            circleWeight: CircleWeight,
            wanderWeight: WanderWeight,
            wanderJitter: WanderJitter);
    }
}
