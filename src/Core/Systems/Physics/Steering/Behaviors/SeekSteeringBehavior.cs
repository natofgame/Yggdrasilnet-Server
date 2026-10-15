using System.Numerics;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering.Behaviors;

internal sealed class SeekSteeringBehavior : ISteeringBehavior {
    public void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer) {
        if (!context.HasDirection) {
            return;
        }

        steer += context.Direction * agent.Movement.Steering.SeekWeight;
    }
}