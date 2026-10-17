using System.Numerics;
using Xunit;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.Systems.Ai;
using Yggdrasilnet.Server.Core.Systems.Physics;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Tests;

public sealed class SteeringRegressionTests {
    [Fact]
    public void NeighborAvoidance_PrioritizesClosestNeighbors_NotFirstScanned() {
        var systems = new SystemPipeline();
        systems.Add(new MovementSystem());
        var dimension = CreateDimension(systems);

        var primary = SpawnSteeringAgent(dimension, new Vector3(0f, 0f, 0f), moveSpeed: 1f, avoidRadius: 5f,
            avoidWeight: 1f);

        for (var i = 0; i < 10; i++) {
            SpawnSteeringAgent(dimension, new Vector3(-4.9f, 0f, -4.9f + i * 0.02f), moveSpeed: 0f, avoidRadius: 0f,
                avoidWeight: 0f);
        }

        SpawnSteeringAgent(dimension, new Vector3(0.5f, 0f, 0f), moveSpeed: 0f, avoidRadius: 0f, avoidWeight: 0f);

        dimension.Update(0.1f);

        Assert.True(primary.Get<MovementComponent>().Velocity.X < 0f);
    }

    [Fact]
    public void GroupCoordinator_AssignsMixedHoldDirectionsAndNonUniformRanges() {
        var dimension = CreateDimension(new SystemPipeline());
        var coordinator = new AiGroupCoordinator();

        var target = dimension.Spawn("test_player", Vector3.Zero);
        target.Set(new InputComponent());

        var brains = new List<BrainComponent>();
        for (var i = 0; i < 6; i++) {
            var mob = dimension.Spawn("test_mob", new Vector3(5f + i, 0f, 0f));
            var brain = mob.Set(new BrainComponent {
                TargetEntityId = target.UniqueId,
                TargetDirection = new Vector3(1f, 0f, 0f),
                TargetDistance = 5f
            });
            brains.Add(brain);
        }

        coordinator.Update(dimension, 0.1f);

        var distinctDirections = brains.Select(b => b.HoldOrbitDirection).Distinct().ToArray();
        Assert.Contains(1f, distinctDirections);
        Assert.Contains(-1f, distinctDirections);

        var distinctRangeOffsets = brains.Select(b => MathF.Round(b.HoldRadiusOffset, 3)).Distinct().ToArray();
        Assert.True(distinctRangeOffsets.Length > 1);

        var distinctSlotAngles = brains.Select(b => MathF.Round(b.SlotAngle, 3)).Distinct().ToArray();
        Assert.Equal(brains.Count, distinctSlotAngles.Length);
    }

    private static Dimension CreateDimension(SystemPipeline systems) {
        var world = new World(null!, new BehaviorPackRegistry());
        return world.CreateDimension("test", systems, true);
    }

    private static Entity SpawnSteeringAgent(Dimension dimension, Vector3 position, float moveSpeed,
        float avoidRadius, float avoidWeight) {
        var entity = dimension.Spawn("test_agent", position);
        entity.Set(new InputComponent());
        entity.Set(new MovementComponent {
            Steering = new SteeringDefinition {
                MoveSpeed = moveSpeed,
                AvoidRadius = avoidRadius,
                AvoidWeight = avoidWeight,
                SeekWeight = 0f,
                CircleWeight = 0f,
                WanderWeight = 0f
            }
        });
        return entity;
    }
}
