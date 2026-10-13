using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;

public sealed class HoldBehavior : AiBehavior {
    public float Speed { get; set; } = 4f;
    public float BaseRadius { get; set; } = 3.5f;
    public float PrudenceRadius { get; set; } = 3f;
    public float ArriveTolerance { get; set; } = 0.6f;
    public float OrbitSpeed { get; set; } = 2.2f;
    public float OrbitCircleWeight { get; set; } = 0.55f;
    public float OrbitWanderWeight { get; set; } = 0.25f;
    public float OrbitWanderJitter { get; set; } = 0.8f;
    public float SeparationRadius { get; set; } = 1.15f;

    public override AiCombatState State => AiCombatState.Reposition;
    public override float TransitionLockDuration => 0.8f;

    public override float Evaluate(in AiContext context, in BrainComponent ai) {
        if (!context.HasTarget || context.HasAttackToken) {
            return 0f;
        }

        var allyFactor = Math.Clamp(context.AlliesNearby / 4f, 0f, 1f);
        return 0.4f + context.Prudence01 * 0.3f + allyFactor * 0.2f;
    }

    public override AiIntent BuildIntent(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt
    ) {
        if (!context.HasTarget || !context.HasDirection) {
            return AiIntent.Stop();
        }

        var radius = BaseRadius + context.Prudence01 * PrudenceRadius + ai.HoldRadiusOffset;
        radius = MathF.Max(MathF.Max(0.25f, SeparationRadius), radius);

        var targetPos = entity.Position + ai.TargetDirection * ai.TargetDistance;
        var slot = targetPos + new Vector3(MathF.Cos(ai.SlotAngle), 0f, MathF.Sin(ai.SlotAngle)) * radius;

        var toSlot = slot - entity.Position;
        toSlot.Y = 0f;
        var dist = toSlot.Length();

        var orbitDirection = ai.HoldOrbitDirection is 1f or -1f ? ai.HoldOrbitDirection : 1f;

        if (dist <= ArriveTolerance) {
            var toTarget = context.TargetDirection;
            var tangent = new Vector2(-toTarget.Y, toTarget.X) * orbitDirection;
            var radialError = context.TargetDistance - radius;
            var radialStrength = Math.Clamp(radialError / MathF.Max(0.1f, radius), -0.75f, 0.75f);
            var move = tangent + toTarget * radialStrength;
            if (move.LengthSquared() > 1f) {
                move = Vector2.Normalize(move);
            }

            var speedFactor = 0.85f + context.Impulsivity01 * 0.25f;
            return AiIntent.Move(
                move,
                OrbitSpeed * speedFactor,
                circleRadius: radius,
                circleDirection: orbitDirection,
                circleWeight: OrbitCircleWeight,
                wanderWeight: OrbitWanderWeight,
                wanderJitter: OrbitWanderJitter);
        }

        var dir = toSlot / dist;
        var clamp = Math.Clamp(dist / 2f, 0.3f, 1f);
        return AiIntent.Move(
            new Vector2(dir.X, dir.Z),
            Speed * clamp,
            circleRadius: radius,
            circleDirection: orbitDirection,
            circleWeight: OrbitCircleWeight * 0.25f,
            wanderWeight: OrbitWanderWeight * 0.25f,
            wanderJitter: OrbitWanderJitter);
    }
}
