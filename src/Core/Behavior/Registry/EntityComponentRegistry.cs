using System.Text.Json;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.Behavior.Impl;

namespace Yggdrasilnet.Server.Core.Behavior.Registry;

public sealed class EntityComponentRegistry {
    private readonly Dictionary<string, Func<JsonElement, JsonSerializerOptions, IBehavior>> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    public EntityComponentRegistry Register<TBehavior>(string componentId)
        where TBehavior : class, IBehavior {
        if (string.IsNullOrWhiteSpace(componentId)) {
            throw new ArgumentException("Component id cannot be empty.", nameof(componentId));
        }

        _factories[componentId] = static (json, options) => {
            return JsonSerializer.Deserialize<TBehavior>(json.GetRawText(), options)
                   ?? throw new InvalidDataException($"Unable to deserialize behavior '{typeof(TBehavior).Name}'.");
        };

        return this;
    }

    public EntityComponentRegistry Register(string componentId,
        Func<JsonElement, JsonSerializerOptions, IBehavior> factory) {
        if (string.IsNullOrWhiteSpace(componentId)) {
            throw new ArgumentException("Component id cannot be empty.", nameof(componentId));
        }

        _factories[componentId] = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    public IBehavior CreateBehavior(string componentId, JsonElement componentJson, JsonSerializerOptions options) {
        if (!_factories.TryGetValue(componentId, out var factory)) {
            throw new InvalidDataException($"Unknown component behavior id '{componentId}'.");
        }

        return factory(componentJson, options);
    }

    public static EntityComponentRegistry CreateDefault() {
        return new EntityComponentRegistry()
            .Register<InputComponentBehavior>("input")
            .Register<MovementComponentBehavior>("movement")
            .Register<BrainComponentBehavior>("brain")
            .Register<CollisionComponentBehavior>("collision")
            .Register<LivingComponentBehavior>("living")
            .Register<ProjectileMotionComponentBehavior>("projectile_motion")
            .Register<ProjectileImpactComponentBehavior>("projectile_impact")
            .Register<CombatComponentBehavior>("combat")
            .RegisterSpellComponents();
    }
}
