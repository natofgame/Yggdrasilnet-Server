using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Behavior.Impl;

public sealed class MovementComponentBehavior : ComponentBehavior<MovementComponent> {
    public float Speed { get; set; } = 4f;
    public SteeringDefinition Steering { get; set; } = new();

    protected override MovementComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsurePositive(Speed, "movement.speed");
        var steering = BuildSteering(spawnPosition);

        return new MovementComponent {
            Speed = Speed,
            Velocity = Vector3.Zero,
            Steering = steering
        };
    }

    private SteeringDefinition BuildSteering(Vector3 spawnPosition) {
        var source = Steering;

        BehaviorValidation.EnsureNonNegative(
            (source.AvoidRadius, "movement.steering.avoidRadius"),
            (source.AvoidWeight, "movement.steering.avoidWeight"),
            (source.SeekWeight, "movement.steering.seekWeight"),
            (source.CircleRadius, "movement.steering.circleRadius"),
            (source.CircleWeight, "movement.steering.circleWeight"),
            (source.WanderWeight, "movement.steering.wanderWeight"),
            (source.WanderJitter, "movement.steering.wanderJitter"),
            (source.RoamRadius, "movement.steering.roamRadius")
        );

        BehaviorValidation.EnsureFinite(
            (source.CircleDirection, "movement.steering.circleDirection"),
            (source.MoveSpeed, "movement.steering.moveSpeed"),
            (source.WanderAngle, "movement.steering.wanderAngle"),
            (source.PunchOverrideSpeed, "movement.steering.punchOverrideSpeed"),
            (source.PunchTimer, "movement.steering.punchTimer"),
            (source.PunchFreezeTimer, "movement.steering.punchFreezeTimer"),
            (source.DashSpeed, "movement.steering.dashSpeed"),
            (source.DashTimer, "movement.steering.dashTimer")
        );

        return new SteeringDefinition {
            AvoidRadius = source.AvoidRadius,
            AvoidWeight = source.AvoidWeight,
            SeekWeight = source.SeekWeight,
            MoveSpeed = source.MoveSpeed,
            CircleRadius = source.CircleRadius,
            CircleWeight = source.CircleWeight,
            CircleDirection = source.CircleDirection,
            WanderWeight = source.WanderWeight,
            WanderJitter = source.WanderJitter,
            WanderAngle = source.WanderAngle,
            RoamRadius = source.RoamRadius,
            SpawnX = spawnPosition.X,
            SpawnZ = spawnPosition.Z,
            HasPunch = source.HasPunch,
            PunchDirection = source.PunchDirection,
            PunchOverrideSpeed = source.PunchOverrideSpeed,
            PunchTimer = source.PunchTimer,
            PunchFreezeTimer = source.PunchFreezeTimer,
            HasDash = source.HasDash,
            DashDirection = source.DashDirection,
            DashSpeed = source.DashSpeed,
            DashTimer = source.DashTimer,
            InputDirection = source.InputDirection,
            CanPunch = source.CanPunch
        };
    }
}
