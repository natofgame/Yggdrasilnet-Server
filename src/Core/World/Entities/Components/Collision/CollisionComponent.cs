using System.Numerics;
using Yggdrasilnet.Maths.Collision;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Collision;

public class CollisionComponent : IComponent {
    public bool IsTrigger;
    public CollisionLayer Layer;
    public CollisionLayer Mask;
    public float X;
    public float Y;
    public float Z;

    public Vector3 Size => new(X, Y, Z);

    public bool CanCollide(CollisionComponent other) {
        var aToB = (Mask & other.Layer) != 0;
        var bToA = (other.Mask & Layer) != 0;
        return aToB && bToA;
    }

    public BoundingBoxes GetWorldAabb(Vector3 position) {
        return BoundingBoxes.From(position, Size);
    }

    public BoundingBoxes GetSweptWorldAabb(Vector3 from, Vector3 to) {
        var start = GetWorldAabb(from);
        if (from == to) {
            return start;
        }

        return start.Encapsulate(GetWorldAabb(to));
    }
}