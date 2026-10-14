using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Behavior.Behaviors.Entity;

public sealed class InputComponentBehavior : ComponentBehavior<InputComponent> {
    public float X { get; set; }
    public float Z { get; set; }

    protected override InputComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsureFinite(X, "input.x");
        BehaviorValidation.EnsureFinite(Z, "input.z");

        return new InputComponent {
            X = X,
            Z = Z
        };
    }
}
