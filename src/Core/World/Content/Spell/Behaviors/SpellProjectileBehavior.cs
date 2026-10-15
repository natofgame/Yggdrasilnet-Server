using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Content.Spell.Behaviors;

public sealed class SpellProjectileChain {
    public int MaxBounces { get; set; } = 3;
    public float Radius { get; set; } = 6f;
    public float DamageMultiplierPerBounce { get; set; } = 0.8f;

    public void Validate() {
        if (MaxBounces < 0) {
            throw new InvalidDataException("projectile.chain.maxBounces must be >= 0.");
        }

        BehaviorValidation.EnsurePositive(Radius, "projectile.chain.radius");
        BehaviorValidation.EnsureNonNegative(DamageMultiplierPerBounce, "projectile.chain.damageMultiplierPerBounce");
    }
}

public sealed class SpellProjectileBehavior : IBehavior {
    public string EntityDefinitionId { get; set; } = string.Empty;
    public float Speed { get; set; } = 12f;
    public float Lifetime { get; set; } = 2f;
    public int MaxHits { get; set; } = 1;
    public float SpawnForward { get; set; } = 0.3f;
    public float SpawnHeight { get; set; } = 1f;
    public bool AreaDamageEnabled { get; set; }
    public float AreaDamageRadius { get; set; }
    public bool AutoTargetEnabled { get; set; }
    public float AutoTargetInfluence { get; set; } = 1f;
    public float AutoTargetRange { get; set; }
    public SpellProjectileChain? Chain { get; set; }

    void IBehavior.Apply(Dimension dimension, int entityId) { }

    public void Validate() {
        if (string.IsNullOrWhiteSpace(EntityDefinitionId)) {
            throw new InvalidDataException("projectile.entityDefinitionId is required.");
        }

        BehaviorValidation.EnsurePositive(Speed, "projectile.speed");
        BehaviorValidation.EnsurePositive(Lifetime, "projectile.lifetime");
        BehaviorValidation.EnsureNonNegative(
            (AreaDamageRadius, "projectile.areaDamageRadius"),
            (AutoTargetInfluence, "projectile.autoTargetInfluence"),
            (AutoTargetRange, "projectile.autoTargetRange"));
        BehaviorValidation.EnsureFinite(SpawnForward, "projectile.spawnForward");
        BehaviorValidation.EnsureFinite(SpawnHeight, "projectile.spawnHeight");
        BehaviorValidation.EnsureFinite(
            (AreaDamageRadius, "projectile.areaDamageRadius"),
            (AutoTargetInfluence, "projectile.autoTargetInfluence"),
            (AutoTargetRange, "projectile.autoTargetRange"));
        if (AreaDamageEnabled && AreaDamageRadius <= 0f) {
            throw new InvalidDataException("projectile.areaDamageRadius must be > 0 when areaDamageEnabled is true.");
        }

        if (MaxHits < 1) {
            throw new InvalidDataException("projectile.maxHits must be >= 1.");
        }

        Chain?.Validate();
    }
}