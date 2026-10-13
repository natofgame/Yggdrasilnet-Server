using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Effect.Handlers;

public interface ISpellEffectHandler {
    void Apply(Dimension world, SpellCastContext context, Entity actor, float amount, float duration);
}
