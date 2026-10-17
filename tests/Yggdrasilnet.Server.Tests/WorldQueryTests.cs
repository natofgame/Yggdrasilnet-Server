using System.Numerics;
using Xunit;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Tests;

public sealed class WorldQueryTests {
    [Fact]
    public void QueryOfThree_ReturnsOnlyEntitiesWithAllComponents_AcrossDimensions() {
        var world = new World(null!, new BehaviorPackRegistry());
        var overworld = world.CreateDimension("overworld", isMain: true);
        var nether = world.CreateDimension("nether");

        var full = overworld.Spawn("test", Vector3.Zero);
        full.Set(new LivingComponent());
        full.Set(new MovementComponent());
        full.Set(new InputComponent());

        var partial = overworld.Spawn("test", Vector3.Zero);
        partial.Set(new LivingComponent());
        partial.Set(new MovementComponent());

        var otherDimension = nether.Spawn("test", Vector3.Zero);
        otherDimension.Set(new LivingComponent());
        otherDimension.Set(new MovementComponent());
        otherDimension.Set(new InputComponent());

        var ids = world.Query<LivingComponent, MovementComponent, InputComponent>()
            .Select(entity => entity.UniqueId)
            .ToHashSet();

        Assert.Equal([full.UniqueId, otherDimension.UniqueId], ids.Order());
    }
}
