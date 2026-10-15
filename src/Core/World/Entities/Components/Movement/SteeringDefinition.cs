using System.Numerics;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

public class SteeringDefinition {
    public Vector2 DashDirection;
    public float DashSpeed;
    public float DashTimer;

    public bool HasDash;
    public bool HasPunch;

    public Vector2 InputDirection;
    public Vector2 PunchDirection;
    public float PunchFreezeTimer;

    public float PunchOverrideSpeed;
    public float PunchTimer;
    public float AvoidRadius { get; set; }
    public float AvoidWeight { get; set; } = 1f;

    public float SeekWeight { get; set; } = 1f;
    public float MoveSpeed { get; set; }

    public float CircleRadius { get; set; }
    public float CircleWeight { get; set; } = 1f;

    public float CircleDirection { get; set; } = 1f;

    public float WanderWeight { get; set; }

    public float WanderJitter { get; set; } = 1f;

    public float WanderAngle { get; set; }

    public float RoamRadius { get; set; }
    public float SpawnX { get; set; }
    public float SpawnZ { get; set; }

    public bool CanPunch { get; set; } = true;
}