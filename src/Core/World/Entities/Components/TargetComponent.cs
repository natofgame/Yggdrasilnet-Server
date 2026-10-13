using System.Numerics;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components;

public class TargetComponent : IComponent {
    public Vector3 Direction;
    public float Distance;
    public int? EntityId;
    public float HealthRatio;

    public float Range;
    public float RefreshInterval;
    public float RefreshTimer;

    public static TargetComponent CreateDefault() {
        return new TargetComponent {
            EntityId = null,
            Distance = 0f,
            Direction = Vector3.Zero,
            HealthRatio = 0f,
            Range = 20f,
            RefreshInterval = 0.2f,
            RefreshTimer = 0f
        };
    }
}
