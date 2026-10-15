using System.Numerics;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering.Behaviors;

internal sealed class CircleSteeringBehavior : ISteeringBehavior {
    public void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer) {
        if (!context.HasDirection) {
            return;
        }

        var steering = agent.Movement.Steering;
        var direction = context.Direction;
        var radialError = context.Distance - steering.CircleRadius;
        var radialStrength = MathF.Min(MathF.Abs(radialError), 1f) * MathF.Sign(radialError);
        steer += direction * radialStrength * steering.CircleWeight;

        var tangent = new Vector2(-direction.Y, direction.X) * steering.CircleDirection;
        steer += tangent * steering.CircleWeight;
    }
}