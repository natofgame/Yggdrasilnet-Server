using System.Numerics;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

/// <summary>
///  Represents the AI's combat state.
/// </summary>
public enum AiCombatState {
    Idle,
    Approach,
    Pressure,
    Reposition,
    Retreat
}

/// <summary>
///  Represents the AI's intent type.
/// </summary>
public enum AiIntentType {
    None,
    Stop,
    Move,
    Cast
}

/// <summary>
///  Represents the AI's context.
/// </summary>
public readonly struct AiContext {
    public bool HasTarget { get; init; }
    public float TargetDistance { get; init; }
    public Vector2 TargetDirection { get; init; }
    public float HealthRatio { get; init; }
    public float TargetHealthRatio { get; init; }
    public bool HasAttackToken { get; init; }
    public float AttackCooldown { get; init; }
    public int AlliesNearby { get; init; }

    public float Prudence01 { get; init; }
    public float Courage01 { get; init; }
    public float Aggressivity01 { get; init; }
    public float Impulsivity01 { get; init; }
    public float Curiosity01 { get; init; }

    public bool HasDirection => TargetDirection.LengthSquared() > 0.0001f;
}

/// <summary>
///  Represents the AI's intent.'
/// </summary>
public readonly struct AiIntent {
    public AiIntentType Type { get; init; }
    public Vector2 MoveDirection { get; init; }
    public float MoveSpeed { get; init; }
    public float SeekWeight { get; init; }
    public float CircleRadius { get; init; }
    public float CircleDirection { get; init; }
    public float CircleWeight { get; init; }
    public float WanderWeight { get; init; }
    public float WanderJitter { get; init; }
    public bool HasDash { get; init; }
    public Vector2 DashDirection { get; init; }
    public float DashSpeed { get; init; }
    public float DashDuration { get; init; }
    public bool ZeroVelocity { get; init; }
    public int SpellIndex { get; init; }

    public static AiIntent Stop(bool zeroVelocity = false) {
        return new AiIntent {
            Type = AiIntentType.Stop,
            ZeroVelocity = zeroVelocity
        };
    }

    public static AiIntent Cast(int spellIndex) {
        return new AiIntent {
            Type = AiIntentType.Cast,
            SpellIndex = spellIndex,
            ZeroVelocity = true
        };
    }

    public static AiIntent Move(
        Vector2 direction,
        float speed,
        float seekWeight = 0f,
        float circleRadius = 0f,
        float circleDirection = 1f,
        float circleWeight = 0f,
        float wanderWeight = 0f,
        float wanderJitter = 1f,
        bool hasDash = false,
        Vector2 dashDirection = default,
        float dashSpeed = 0f,
        float dashDuration = 0f,
        bool zeroVelocity = false
    ) {
        if (direction.LengthSquared() > 1f) {
            direction = Vector2.Normalize(direction);
        }

        if (hasDash && dashDirection.LengthSquared() > 1f) {
            dashDirection = Vector2.Normalize(dashDirection);
        }

        return new AiIntent {
            Type = AiIntentType.Move,
            MoveDirection = direction,
            MoveSpeed = MathF.Max(0f, speed),
            SeekWeight = seekWeight,
            CircleRadius = MathF.Max(0f, circleRadius),
            CircleDirection = circleDirection is 1f or -1f ? circleDirection : 1f,
            CircleWeight = MathF.Max(0f, circleWeight),
            WanderWeight = MathF.Max(0f, wanderWeight),
            WanderJitter = MathF.Max(0f, wanderJitter),
            HasDash = hasDash,
            DashDirection = dashDirection,
            DashSpeed = MathF.Max(0f, dashSpeed),
            DashDuration = MathF.Max(0f, dashDuration),
            ZeroVelocity = zeroVelocity
        };
    }
}
