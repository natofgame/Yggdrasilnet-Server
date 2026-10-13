using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Effect.Handlers;

public class PunchEffectHandler : ISpellEffectHandler {
    public void Apply(Dimension world, SpellCastContext context, Entity actor, float amount, float duration) {
        if (!actor.TryGet<MovementComponent>(out var movement)) {
            return;
        }

        var direction = new Vector2(context.Direction.X, context.Direction.Z);
        if (direction.LengthSquared() <= 0.0001f) {
            return;
        }

        direction = Vector2.Normalize(direction);
        var steering = movement.Steering;

        if (!steering.CanPunch) {
            return;
        }

        steering.HasPunch = true;
        steering.PunchDirection = direction;
        steering.PunchOverrideSpeed = 20f;
        steering.PunchTimer = 0.06f;
        steering.PunchFreezeTimer = 0.3f;

        movement.Steering = steering;
        actor.Set(movement);
    }
}
