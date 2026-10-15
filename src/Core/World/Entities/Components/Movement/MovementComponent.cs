using System.Numerics;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

public class MovementComponent : IComponent {
    public float Speed;

    public SteeringDefinition Steering = new();
    public Vector3 Velocity;
}