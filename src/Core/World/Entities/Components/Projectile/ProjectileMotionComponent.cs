using System.Numerics;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;

public class ProjectileMotionComponent : IComponent {
    public bool AutoTargetEnabled;
    public float AutoTargetInfluence;
    public float AutoTargetRange;
    public Vector3 Direction;
    public float RemainingLifetimeSeconds;
    public float Speed;
    public Vector3 Velocity;
}