using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;

public class ProjectileImpactComponent : IComponent {
    public bool AreaDamageEnabled;
    public float AreaDamageRadius;
    public float Damage;
    public List<int>? HitEntityIds;
    public int OwnerEntityId;
    public int RemainingHits;

    public string SpellId = string.Empty;
    public CollisionLayer TargetLayer;
}
