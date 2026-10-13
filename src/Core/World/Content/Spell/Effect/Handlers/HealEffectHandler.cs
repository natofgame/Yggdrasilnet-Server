using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Effect.Handlers;

public class HealEffectHandler : ISpellEffectHandler {
    public void Apply(Dimension world, SpellCastContext context, Entity actor, float amount, float duration) {
        if (!actor.TryGet<LivingComponent>(out var health)) {
            return;
        }

        health.Current = MathF.Min(health.Max, health.Current + amount);
        actor.Set(health);
    }
}
