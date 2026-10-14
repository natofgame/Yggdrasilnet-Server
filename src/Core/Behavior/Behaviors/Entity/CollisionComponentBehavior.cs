using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;

namespace Yggdrasilnet.Server.Core.Behavior.Behaviors.Entity;

public sealed class CollisionComponentBehavior : ComponentBehavior<CollisionComponent> {
    public float X { get; set; } = 0.8f;
    public float Y { get; set; } = 1.8f;
    public float Z { get; set; } = 0.8f;
    public bool IsTrigger { get; set; }
    public string Layer { get; set; } = "world";
    public string Mask { get; set; } = "all";

    protected override CollisionComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsurePositive(X, "collision.x");
        BehaviorValidation.EnsurePositive(Y, "collision.y");
        BehaviorValidation.EnsurePositive(Z, "collision.z");

        return new CollisionComponent {
            X = X,
            Y = Y,
            Z = Z,
            IsTrigger = IsTrigger,
            Layer = CollisionLayerParser.Parse(Layer),
            Mask = CollisionLayerParser.Parse(Mask)
        };
    }
}
