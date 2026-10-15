using System.Numerics;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering.Behaviors;

internal sealed class NeighborAvoidanceSteeringBehavior : ISteeringBehavior {
    public void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer) {
        var steering = agent.Movement.Steering;
        if (!context.EnableWanderAndAvoid || !HasAvoidance(steering)) {
            return;
        }

        context.ApplyNeighborAvoidance(agent, ref steer);
    }

    private static bool HasAvoidance(SteeringDefinition steering) {
        return steering.AvoidRadius > 0f && steering.AvoidWeight != 0f;
    }
}