using System.Text.Json;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

namespace Yggdrasilnet.Server.Core.Behavior.Registry;

public static class SpellComponentRegistryExtensions {
    public static EntityComponentRegistry RegisterSpellComponents(this EntityComponentRegistry registry) {
        return registry
            .RegisterValidated<SpellMetaBehavior>("meta", b => b.Validate())
            .RegisterValidated<SpellTargetingBehavior>("targeting", b => b.Validate())
            .RegisterValidated<SpellTimingBehavior>("timing", b => b.Validate())
            .RegisterValidated<SpellCooldownBehavior>("cooldown", b => b.Validate())
            .RegisterValidated<SpellChargesBehavior>("charges", b => b.Validate())
            .RegisterValidated<SpellStacksBehavior>("stacks", b => b.Validate())
            .RegisterValidated<SpellEffectsBehavior>("effects", b => b.Validate())
            .RegisterValidated<SpellProjectileBehavior>("projectile", b => b.Validate());
    }

    private static EntityComponentRegistry RegisterValidated<TBehavior>(
        this EntityComponentRegistry registry,
        string componentId,
        Action<TBehavior> validate
    ) where TBehavior : class, IBehavior {
        return registry.Register(componentId, (json, options) => {
            var behavior = JsonSerializer.Deserialize<TBehavior>(json.GetRawText(), options)
                           ?? throw new InvalidDataException(
                               $"Unable to deserialize behavior '{typeof(TBehavior).Name}'.");
            validate(behavior);
            return behavior;
        });
    }
}
