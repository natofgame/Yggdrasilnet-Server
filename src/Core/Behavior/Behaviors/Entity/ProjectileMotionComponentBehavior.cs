using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;

namespace Yggdrasilnet.Server.Core.Behavior.Behaviors.Entity;

public sealed class ProjectileMotionComponentBehavior : ComponentBehavior<ProjectileMotionComponent> {
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public float VelocityZ { get; set; }
    public float Lifetime { get; set; } = 2f;
    public bool AutoTargetEnabled { get; set; }
    public float AutoTargetInfluence { get; set; } = 1f;
    public float AutoTargetRange { get; set; }

    /// <inheritdoc />
    protected override ProjectileMotionComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsurePositive(Lifetime, "projectile_motion.lifetime");
        BehaviorValidation.EnsureFinite(
            (VelocityX, "projectile_motion.velocityX"),
            (VelocityY, "projectile_motion.velocityY"),
            (VelocityZ, "projectile_motion.velocityZ"));
        BehaviorValidation.EnsureNonNegative(
            (AutoTargetInfluence, "projectile_motion.autoTargetInfluence"),
            (AutoTargetRange, "projectile_motion.autoTargetRange"));
        BehaviorValidation.EnsureFinite(
            (AutoTargetInfluence, "projectile_motion.autoTargetInfluence"),
            (AutoTargetRange, "projectile_motion.autoTargetRange"));

        var velocity = new Vector3(VelocityX, VelocityY, VelocityZ);
        var direction = ResolveDirection(velocity);
        var speed = ResolvePlanarSpeed(velocity);

        return new ProjectileMotionComponent {
            Velocity = velocity,
            Direction = direction,
            Speed = speed,
            AutoTargetEnabled = AutoTargetEnabled,
            AutoTargetInfluence = AutoTargetInfluence,
            AutoTargetRange = AutoTargetRange,
            RemainingLifetimeSeconds = Lifetime
        };
    }

    /// <summary>
    ///     Calculates the direction of the projectile.
    /// </summary>
    /// <param name="velocity"></param>
    /// <returns></returns>
    private static Vector3 ResolveDirection(Vector3 velocity) {
        var flat = velocity with { Y = 0f };
        return flat.LengthSquared() > 0.0001f
            ? Vector3.Normalize(flat)
            : Vector3.UnitZ;
    }

    /// <summary>
    ///     Calculates the speed of the projectile in the plane.
    /// </summary>
    /// <param name="velocity"></param>
    /// <returns></returns>
    private static float ResolvePlanarSpeed(Vector3 velocity) {
        var flat = velocity with { Y = 0f };
        return MathF.Sqrt(flat.LengthSquared());
    }
}