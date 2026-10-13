using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;

namespace Yggdrasilnet.Server.Core.Behavior.Impl;

public sealed class ProjectileImpactComponentBehavior : ComponentBehavior<ProjectileImpactComponent> {
    public float Damage { get; set; } = 10f;
    public bool AreaDamageEnabled { get; set; }
    public float AreaDamageRadius { get; set; }
    public int MaxHits { get; set; } = 1;
    public string TargetLayer { get; set; } = "all";

    protected override ProjectileImpactComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsureNonNegative(Damage, "projectile_impact.damage");
        BehaviorValidation.EnsureNonNegative(AreaDamageRadius, "projectile_impact.areaDamageRadius");
        BehaviorValidation.EnsureFinite(AreaDamageRadius, "projectile_impact.areaDamageRadius");
        if (AreaDamageEnabled && AreaDamageRadius <= 0f) {
            throw new InvalidDataException(
                "projectile_impact.areaDamageRadius must be > 0 when areaDamageEnabled is true.");
        }

        if (MaxHits < 1) {
            throw new InvalidDataException("projectile_impact.maxHits must be >= 1.");
        }

        return new ProjectileImpactComponent {
            OwnerEntityId = -1,
            TargetLayer = CollisionLayerParser.Parse(TargetLayer),
            Damage = Damage,
            AreaDamageEnabled = AreaDamageEnabled,
            AreaDamageRadius = AreaDamageRadius,
            RemainingHits = MaxHits,
            HitEntityIds = null
        };
    }
}
