using System.Numerics;
using Yggdrasilnet.Server.Simulation.World.Component;

namespace Yggdrasilnet.Server.Simulation.Content.Ai.Behaviors;

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

    public override float Score(AiComponent ai) {
        if (!ai.HasTarget || ai.HasAttackToken) {
            return 0f;
        }

        return 0.15f;
    }

    public override void Tick(World.World world, World.Entity entity, AiComponent ai, SteeringComponent steering, float dt) {
        if (steering.AvoidRadius < SeparationRadius) {
            steering.AvoidRadius = SeparationRadius;
        }

        var radius = BaseRadius + Math.Clamp(ai.Prudence / 10f, 0f, 1f) * PrudenceRadius + ai.HoldRadiusOffset;
        radius = MathF.Max(0.25f, radius);

        var targetPos = entity.Position + ai.TargetDirection * ai.TargetDistance;
        var slot = targetPos + new Vector3(MathF.Cos(ai.SlotAngle), 0f, MathF.Sin(ai.SlotAngle)) * radius;

        var toSlot = slot - entity.Position;
        toSlot.Y = 0f;
        var dist = toSlot.Length();

        var orbitDirection = ai.HoldOrbitDirection is 1f or -1f ? ai.HoldOrbitDirection : 1f;

        if (dist <= ArriveTolerance) {
            steering.CircleRadius = radius;
            steering.CircleDirection = orbitDirection;
            steering.CircleWeight = OrbitCircleWeight;
            steering.WanderWeight = OrbitWanderWeight;
            steering.WanderJitter = OrbitWanderJitter;
            steering.MoveSpeed = OrbitSpeed * (0.85f + Clamp01(ai.Impulsivity / 10f) * 0.25f);
            return;
        }

        var dir = toSlot / dist;
        steering.InputDirection = new Vector2(dir.X, dir.Z);
        steering.CircleRadius = radius;
        steering.CircleDirection = orbitDirection;
        steering.CircleWeight = OrbitCircleWeight * 0.25f;
        steering.WanderWeight = OrbitWanderWeight * 0.25f;
        steering.WanderJitter = OrbitWanderJitter;
        steering.MoveSpeed = Speed * Math.Clamp(dist / 2f, 0.3f, 1f);
    }

    private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
}
