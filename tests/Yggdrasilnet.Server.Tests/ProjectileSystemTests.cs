using System.Numerics;
using Xunit;
using Yggdrasilnet.Network.Enums;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.Systems.Combat;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Projectile;

namespace Yggdrasilnet.Server.Tests;

public sealed class ProjectileSystemTests {
    [Fact]
    public void ProjectileSystem_MaxHitsOne_AppliesDamageAndDespawns() {
        var dimension = CreateDimension();
        var caster = SpawnLiving(dimension, Vector3.Zero, CollisionLayer.Player);
        var target = SpawnLiving(dimension, new Vector3(2f, 0f, 0f), CollisionLayer.Monster);
        var projectile = SpawnProjectile(dimension, caster, new Vector3(0.5f, 0f, 0f), damage: 25f, maxHits: 1);

        dimension.Update(0.05f); // travelling, not yet touching the target
        Assert.Equal(100f, target.Get<LivingComponent>().Current);
        Assert.True(dimension.TryGetEntity(projectile.UniqueId, out _));

        dimension.Update(0.05f); // impact
        Assert.Equal(75f, target.Get<LivingComponent>().Current);
        Assert.False(dimension.TryGetEntity(projectile.UniqueId, out _));
    }

    [Fact]
    public void ProjectileSystem_IgnoresOwner_EvenWhenOverlapping() {
        var dimension = CreateDimension();
        var caster = SpawnLiving(dimension, Vector3.Zero, CollisionLayer.Monster);
        var projectile = SpawnProjectile(dimension, caster, Vector3.Zero, damage: 25f, maxHits: 1);

        dimension.Update(0.05f);

        Assert.Equal(100f, caster.Get<LivingComponent>().Current);
        Assert.True(dimension.TryGetEntity(projectile.UniqueId, out _));
    }

    [Fact]
    public void ProjectileSystem_DespawnsWhenLifetimeExpires() {
        var dimension = CreateDimension();
        var caster = SpawnLiving(dimension, Vector3.Zero, CollisionLayer.Player);
        var projectile = SpawnProjectile(dimension, caster, new Vector3(0.5f, 0f, 0f), damage: 25f, maxHits: 1,
            lifetime: 0.04f);

        dimension.Update(0.05f);

        Assert.False(dimension.TryGetEntity(projectile.UniqueId, out _));
    }

    private static Dimension CreateDimension() {
        var behaviors = new BehaviorPackRegistry();
        var systems = new SystemPipeline();
        systems.Add(new ProjectileSystem(behaviors));

        var world = new World(null!, behaviors);
        return world.CreateDimension("test", systems, true);
    }

    private static Entity SpawnLiving(Dimension dimension, Vector3 position, CollisionLayer layer) {
        var entity = dimension.Spawn("test_living", position);
        entity.Set(new LivingComponent { Current = 100f, Max = 100f });
        entity.Set(new CollisionComponent { Layer = layer, Mask = CollisionLayer.All, X = 0.6f, Y = 1.8f, Z = 0.6f });
        return entity;
    }

    private static Entity SpawnProjectile(Dimension dimension, Entity owner, Vector3 position, float damage,
        int maxHits, float lifetime = 2.5f) {
        var projectile = dimension.Spawn("test_projectile", position);
        projectile.Set(new CollisionComponent {
            Layer = CollisionLayer.Spell,
            Mask = CollisionLayer.All,
            IsTrigger = true,
            X = 0.4f,
            Y = 0.4f,
            Z = 0.4f
        });
        projectile.Set(new ProjectileMotionComponent {
            Direction = Vector3.UnitX,
            Speed = 14f,
            Velocity = Vector3.UnitX * 14f,
            RemainingLifetimeSeconds = lifetime
        });
        projectile.Set(new ProjectileImpactComponent {
            OwnerEntityId = owner.UniqueId,
            TargetLayer = CollisionLayer.Monster,
            Damage = damage,
            RemainingHits = maxHits
        });
        return projectile;
    }
}
