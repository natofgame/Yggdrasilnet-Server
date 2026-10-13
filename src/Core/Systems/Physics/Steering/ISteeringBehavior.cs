using System.Numerics;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering;

internal interface ISteeringBehavior {
    void Apply(SteeringContext context, SteeringAgent agent, ref Vector2 steer);
}
