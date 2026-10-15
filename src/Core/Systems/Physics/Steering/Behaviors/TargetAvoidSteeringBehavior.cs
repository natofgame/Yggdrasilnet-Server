using System.Numerics;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering.Behaviors;

internal sealed class TargetAvoidSteeringBehavior : ISteeringBehavior {
    public void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer) {
        if (!context.HasDirection || !context.EnableWanderAndAvoid) {
            return;
        }

        var steering = agent.Movement.Steering;
        var avoidRadius = steering.AvoidRadius;
        if (avoidRadius <= SteeringConstants.MinDistanceSquared || context.Distance >= avoidRadius) {
            return;
        }

        var inward = Vector2.Dot(steer, context.Direction);
        if (inward <= 0f) {
            return;
        }

        var strength = (avoidRadius - context.Distance) / avoidRadius;
        steer -= context.Direction * inward * strength * steering.AvoidWeight;
    }
}