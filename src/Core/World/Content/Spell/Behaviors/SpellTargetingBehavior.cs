using System.Numerics;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public sealed class SpellTargetingBehavior : IBehavior {
    public float Range { get; set; }
    public float HitBoxMinX { get; set; }
    public float HitBoxMinY { get; set; }
    public float HitBoxMinZ { get; set; }
    public float HitBoxMaxX { get; set; }
    public float HitBoxMaxY { get; set; }
    public float HitBoxMaxZ { get; set; }
    public float? CardinalHitBoxMinX { get; set; }
    public float? CardinalHitBoxMinY { get; set; }
    public float? CardinalHitBoxMinZ { get; set; }
    public float? CardinalHitBoxMaxX { get; set; }
    public float? CardinalHitBoxMaxY { get; set; }
    public float? CardinalHitBoxMaxZ { get; set; }
    public string TargetLayer { get; set; } = "all";

    public Vector3 HitBoxMin => new(HitBoxMinX, HitBoxMinY, HitBoxMinZ);
    public Vector3 HitBoxMax => new(HitBoxMaxX, HitBoxMaxY, HitBoxMaxZ);

    public Vector3? CardinalHitBoxMin =>
        CardinalHitBoxMinX is null || CardinalHitBoxMinY is null || CardinalHitBoxMinZ is null
            ? null
            : new Vector3(CardinalHitBoxMinX.Value, CardinalHitBoxMinY.Value, CardinalHitBoxMinZ.Value);

    public Vector3? CardinalHitBoxMax =>
        CardinalHitBoxMaxX is null || CardinalHitBoxMaxY is null || CardinalHitBoxMaxZ is null
            ? null
            : new Vector3(CardinalHitBoxMaxX.Value, CardinalHitBoxMaxY.Value, CardinalHitBoxMaxZ.Value);

    public CollisionLayer ResolvedTargetLayer => CollisionLayerParser.Parse(TargetLayer);

    void IBehavior.Apply(Dimension dimension, int entityId) { }

    public void Validate() {
        BehaviorValidation.EnsureNonNegative(Range, "targeting.range");
        _ = ResolvedTargetLayer;
    }
}