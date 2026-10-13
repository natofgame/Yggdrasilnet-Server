using System.Numerics;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

public static class AiMotionController {
    private const float DefaultNeighborAvoidRadius = 1.35f;
    private const float DefaultNeighborAvoidWeight = 1.1f;

    public static void Stop(ref InputComponent input, ref MovementComponent movement) {
        Apply(ref input, ref movement, AiIntent.Stop(true));
    }

    public static void Apply(ref InputComponent input, ref MovementComponent movement, in AiIntent intent) {
        Reset(ref input, movement.Steering);

        switch (intent.Type) {
            case AiIntentType.Move:
                ApplyMove(ref input, movement.Steering, intent);
                break;
            case AiIntentType.Cast:
            case AiIntentType.Stop:
            case AiIntentType.None:
            default:
                break;
        }

        if (!intent.ZeroVelocity) {
            return;
        }

        movement.Velocity.X = 0f;
        movement.Velocity.Z = 0f;
    }

    private static void ApplyMove(ref InputComponent input, SteeringDefinition steering, in AiIntent intent) {
        var direction = intent.MoveDirection;
        if (direction.LengthSquared() > 1f) {
            direction = Vector2.Normalize(direction);
        }

        input.Direction = direction;

        steering.MoveSpeed = MathF.Max(0f, intent.MoveSpeed);
        steering.SeekWeight = MathF.Max(0f, intent.SeekWeight);
        if (steering.AvoidRadius <= 0f) {
            steering.AvoidRadius = DefaultNeighborAvoidRadius;
        }

        if (steering.AvoidWeight <= 0f) {
            steering.AvoidWeight = DefaultNeighborAvoidWeight;
        }

        steering.CircleRadius = MathF.Max(0f, intent.CircleRadius);
        steering.CircleDirection = intent.CircleDirection is 1f or -1f ? intent.CircleDirection : 1f;
        steering.CircleWeight = MathF.Max(0f, intent.CircleWeight);
        steering.WanderWeight = MathF.Max(0f, intent.WanderWeight);
        steering.WanderJitter = MathF.Max(0f, intent.WanderJitter);

        if (!intent.HasDash) {
            return;
        }

        steering.HasDash = true;
        steering.DashDirection = intent.DashDirection.LengthSquared() > 1f
            ? Vector2.Normalize(intent.DashDirection)
            : intent.DashDirection;
        steering.DashSpeed = MathF.Max(0.01f, intent.DashSpeed);
        steering.DashTimer = MathF.Max(0f, intent.DashDuration);
    }

    private static void Reset(ref InputComponent input, SteeringDefinition steering) {
        input.Direction = Vector2.Zero;

        steering.InputDirection = Vector2.Zero;
        steering.MoveSpeed = 0f;
        steering.SeekWeight = 0f;
        steering.CircleRadius = 0f;
        steering.CircleWeight = 0f;
        steering.CircleDirection = 1f;
        steering.WanderWeight = 0f;
        steering.WanderJitter = 1f;
        steering.HasDash = false;
        steering.DashTimer = 0f;
    }
}
