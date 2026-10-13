using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;

public sealed class FleeBehavior : AiBehavior {
    public float Speed { get; set; } = 3.5f;
    public float DashMultiplier { get; set; } = 1.8f;
    public float DashDuration { get; set; } = 0.3f;

    public override AiCombatState State => AiCombatState.Retreat;
    public override float TransitionLockDuration => 0.9f;

    public override float Evaluate(in AiContext context, in BrainComponent ai) {
        if (!context.HasTarget) {
            return 0f;
        }

        var fear = context.Prudence01 * (1f - context.Courage01) * (1f - context.HealthRatio);
        if (context.HealthRatio <= 0.35f) {
            fear += (0.35f - context.HealthRatio) * 1.5f;
        }

        if (context.AlliesNearby <= 0) {
            fear += 0.1f * (1f - context.HealthRatio);
        }

        return Math.Clamp(fear, 0f, 1f);
    }

    public override AiIntent BuildIntent(
        Dimension world,
        Entity entity,
        ref BrainComponent ai,
        in AiContext context,
        float dt
    ) {
        var away = new Vector2(-ai.TargetDirection.X, -ai.TargetDirection.Z);
        if (away.LengthSquared() <= 0.0001f) {
            return AiIntent.Stop();
        }

        away = Vector2.Normalize(away);

        return AiIntent.Move(
            away,
            Speed,
            0f,
            hasDash: true,
            dashDirection: away,
            dashSpeed: Speed * DashMultiplier,
            dashDuration: DashDuration);
    }
}
