using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;

namespace Yggdrasilnet.Server.Core.Behavior.Behaviors.Entity;

public sealed class LivingComponentBehavior : ComponentBehavior<LivingComponent> {
    public float Max { get; set; } = 100f;
    public float? Current { get; set; }

    protected override LivingComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsurePositive(Max, "living.max");
        var current = Current ?? Max;
        BehaviorValidation.EnsureFinite(current, "living.current");
        if (current < 0f || current > Max) {
            throw new InvalidDataException("living.current must be between 0 and living.max.");
        }

        return new LivingComponent {
            Max = Max,
            Current = current,
            State = LifeState.Alive,
            DespawnTimer = 0f
        };
    }
}
