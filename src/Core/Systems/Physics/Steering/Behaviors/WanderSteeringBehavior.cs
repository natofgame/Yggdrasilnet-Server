using System.Numerics;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering.Behaviors;

internal sealed class WanderSteeringBehavior : ISteeringBehavior {
    public void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer) {
        var steering = agent.Movement.Steering;
        if (steering.WanderWeight <= 0f) {
            return;
        }

        var jitter = ((float)Random.Shared.NextDouble() * 2f - 1f) * steering.WanderJitter * context.DeltaTime;
        steering.WanderAngle += jitter;
        var wanderDirection = new Vector2(MathF.Cos(steering.WanderAngle), MathF.Sin(steering.WanderAngle));
        steer += wanderDirection * steering.WanderWeight;
    }
}
