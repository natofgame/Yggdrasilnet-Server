using System.Text.Json;
using Yggdrasilnet.Server.Core.Behavior.Behaviors.Entity;
using Yggdrasilnet.Server.Core.Behavior.Core;

namespace Yggdrasilnet.Server.Core.Behavior.Registry;

/// <summary>
///     Registry for entity components.
/// </summary>
public sealed class EntityComponentRegistry {
    private readonly Dictionary<string, Func<JsonElement, JsonSerializerOptions, IBehavior>> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Registers component behaviors.
    /// </summary>
    /// <param name="componentId"></param>
    /// <typeparam name="TBehavior"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidDataException"></exception>
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

    /// <summary>
    ///     Registers component behaviors.
    /// </summary>
    /// <param name="componentId"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public EntityComponentRegistry Register(string componentId,
        Func<JsonElement, JsonSerializerOptions, IBehavior> factory) {
        if (string.IsNullOrWhiteSpace(componentId)) {
            throw new ArgumentException("Component id cannot be empty.", nameof(componentId));
        }

        _factories[componentId] = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>
    ///     Creates a behavior from a component.
    /// </summary>
    /// <param name="componentId"></param>
    /// <param name="componentJson"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public IBehavior CreateBehavior(string componentId, JsonElement componentJson, JsonSerializerOptions options) {
        if (!_factories.TryGetValue(componentId, out var factory)) {
            throw new InvalidDataException($"Unknown component behavior id '{componentId}'.");
        }

        return factory(componentJson, options);
    }

    /// <summary>
    ///     Creates a default registry.
    /// </summary>
    /// <returns></returns>
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