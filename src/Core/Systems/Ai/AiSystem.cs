using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

/// <summary>
///     Handles AI.
/// </summary>
public sealed class AiSystem : ISystem {
    public const string Identifier = "ai";
    private readonly AiGroupCoordinator _groups = new();

    /// <inheritdoc />
    public string Id => Identifier;

    /// <inheritdoc />
    public SystemPhase Phase => SystemPhase.Ai;

    /// <inheritdoc />
    public void Update(Dimension dimension, float dt) {
        if (!float.IsFinite(dt) || dt < 0f) {
            return;
        }

        _groups.Update(dimension, dt);

        foreach (var entity in dimension.Query<BrainComponent>().ToArray()) {
            var brain = entity.Get<BrainComponent>();
            if (!entity.TryGet<LivingComponent>(out var living) || !living.IsAlive) {
                continue;
            }

            if (!entity.TryGet<MovementComponent>(out var movement)) {
                continue;
            }

            if (!entity.TryGet<InputComponent>(out var input)) {
                input = new InputComponent();
                entity.Set(input);
            }

            AiBrain.Tick(dimension, entity, ref brain, living, ref input, ref movement, dt);
            entity.Set(brain);
            entity.Set(input);
            entity.Set(movement);
        }
    }
}