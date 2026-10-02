using System.Numerics;
using Yggdrasilnet.Server.Simulation.World.System.Physic.Steering;
using Yggdrasilnet.Server.Simulation.World.System.Steering;

namespace Yggdrasilnet.Server.Simulation.World.System.Steering.Behaviors;

internal sealed class TargetAvoidSteeringBehavior : ISteeringBehavior {
    public void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer) {
        if (!context.HasDirection || !context.EnableWanderAndAvoid) {
            return;
        }

        var avoidRadius = agent.Steering.AvoidRadius;
        if (avoidRadius <= SteeringConstants.MinDistanceSquared || context.Distance >= avoidRadius) {
            return;
        }

        var inward = Vector2.Dot(steer, context.Direction);
        if (inward <= 0f) {
            return;
        }

        var strength = (avoidRadius - context.Distance) / avoidRadius;
        steer -= context.Direction * inward * strength * agent.Steering.AvoidWeight;
    }
}
