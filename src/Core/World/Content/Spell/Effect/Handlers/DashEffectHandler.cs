using System.Numerics;
using Serilog;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Effect.Handlers;

public class DashEffectHandler : ISpellEffectHandler {
    public void Apply(Dimension world, SpellCastContext context, Entity actor, float amount, float duration) {
        if (!world.TryGetEntity(context.OwnerId, out var caster)) {
            Log.Logger.Information("Cannot find caster");
            return;
        }

        Vector2? direction = null;

        if (actor.TryGet<BrainComponent>(out var ai)) {
            direction = new Vector2(
                ai.TargetDirection.X,
                ai.TargetDirection.Z
            );
        }

        if (!actor.TryGet<MovementComponent>(out var movement)) {
            return;
        }

        var steering = movement.Steering;
        direction ??= steering.InputDirection;

        direction = direction.Value.LengthSquared() > 0.0001f ? Vector2.Normalize(direction.Value) : Vector2.UnitX;
        steering.HasDash = true;
        steering.DashDirection = direction.Value;
        steering.DashSpeed = amount;
        steering.DashTimer = duration;
        movement.Steering = steering;
        actor.Set(movement);
    }
}