using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;

/// <summary>
///     Wanders around randomly.
/// </summary>
public sealed class WanderBehavior : AiBehavior {
    public float Speed { get; set; } = 2f;
    public float Radius { get; set; } = 6f;
    public float ArriveDistance { get; set; } = 0.5f;
    public float IdleDuration { get; set; } = 2f;

    /// <inheritdoc />
    public override AiCombatState State => AiCombatState.Idle;

    /// <inheritdoc />
    public override float TransitionLockDuration => 0.3f;

    /// <inheritdoc />
    public override float Evaluate(in AiContext context, in BrainComponent ai) {
        return context.HasTarget ? 0f : 1f;
    }

    /// <inheritdoc />
    public override AiIntent BuildIntent(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt
    ) {
        if (ai.WanderTarget is not { } target) {
            ai.WanderTarget = PickRandomPoint(entity.Position, Radius);
            return AiIntent.Stop();
        }

        var toTarget = target - entity.Position;
        toTarget.Y = 0f;
        var distance = toTarget.Length();

        if (distance <= ArriveDistance) {
            ai.WanderIdleTimer += dt;
            if (ai.WanderIdleTimer < IdleDuration) {
                return AiIntent.Stop();
            }

            ai.WanderTarget = null;
            ai.WanderIdleTimer = 0f;
            return AiIntent.Stop();
        }

        var dir = toTarget / distance;
        return AiIntent.Move(new Vector2(dir.X, dir.Z), Speed);
    }

    /// <summary>
    ///     Picks a random point within a circle.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private static Vector3 PickRandomPoint(Vector3 origin, float radius) {
        var angle = (float)(Random.Shared.NextDouble() * MathF.Tau);
        var dist = (float)Random.Shared.NextDouble() * radius;
        return origin + new Vector3(MathF.Cos(angle) * dist, 0f, MathF.Sin(angle) * dist);
    }
}