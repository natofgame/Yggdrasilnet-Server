using System.Numerics;
using Yggdrasilnet.Server.Core.Systems.Physics.Steering;
using Yggdrasilnet.Server.Core.Systems.Physics.Steering.Behaviors;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Physics;

/// <summary>
///     The movement system.
/// </summary>
public sealed class MovementSystem : ISystem {
    public const string Identifier = "movement";

    private static readonly ISteeringBehavior[] Behaviors = [
        new SeekSteeringBehavior(),
        new CircleSteeringBehavior(),
        new TargetAvoidSteeringBehavior(),
        new NeighborAvoidanceSteeringBehavior(),
        new WanderSteeringBehavior(),
        new RoamConstraintSteeringBehavior()
    ];

    private readonly SteeringContext _context = new();

    /// <inheritdoc />
    public string Id => Identifier;

    /// <inheritdoc />
    public SystemPhase Phase => SystemPhase.Physics;

    /// <inheritdoc />
    public void Update(Dimension dimension, float dt) {
        _context.Tick++;
        _context.DeltaTime = dt;
        _context.ResetForTick();

        var maxAvoidRadius = 0f;
        var hasAvoidance = false;
        foreach (var entity in dimension.Query<MovementComponent, InputComponent>()) {
            var movement = entity.Get<MovementComponent>();
            _context.SteeredAgents.Add(new SteeringAgent(entity, movement, Flat(entity.Position)));
            var steering = movement.Steering;

            hasAvoidance |= steering.AvoidRadius > 0f && steering.AvoidWeight != 0f;
            if (steering.AvoidRadius > maxAvoidRadius) {
                maxAvoidRadius = steering.AvoidRadius;
            }
        }

        if (_context.SteeredAgents.Count == 0) {
            _context.FinishGridBuild();
            return;
        }

        var cellSize = MathF.Max(0.1f, maxAvoidRadius);
        _context.CellSize = cellSize;
        if (hasAvoidance) {
            _context.BuildSteeringGrid(cellSize);
        }

        foreach (var entity in dimension.Query<InputComponent>())
            _context.PlayerTargets.Add(new PlayerTarget(entity, Flat(entity.Position)));

        if (_context.PlayerTargets.Count == 0) {
            _context.FinishGridBuild();
            ApplyNoPlayerSteering(dt);
            return;
        }

        _context.BuildPlayerGrid(SteeringConstants.PlayerGridCellSize);
        _context.FinishGridBuild();

        var steeredAgents = _context.SteeredAgents;
        foreach (var agent in steeredAgents) {
            if (!_context.TryFindNearestTarget(agent.Position, out var target, out var nearestPlayerDistanceSquared)) {
                continue;
            }

            var updateInterval = ResolveUpdateInterval(nearestPlayerDistanceSquared);
            if (updateInterval > 1 && (_context.Tick + agent.Entity.UniqueId) % updateInterval != 0) {
                continue;
            }

            _context.EnableWanderAndAvoid =
                nearestPlayerDistanceSquared <= SteeringConstants.WanderAvoidDistanceSquared;
            PrepareDirection(agent.Position, target.Position);
            StepEntity(agent, dt);
        }
    }

    /// <summary>
    ///     Applies steering to all entities without a player target.
    /// </summary>
    /// <param name="dt"></param>
    private void ApplyNoPlayerSteering(float dt) {
        _context.EnableWanderAndAvoid = true;
        _context.HasDirection = false;

        var steeredAgents = _context.SteeredAgents;
        foreach (var agent in steeredAgents)
            StepEntity(agent, dt);
    }

    /// <summary>
    ///     Applies steering to a single entity.
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="dt"></param>
    private void StepEntity(SteeringAgent agent, float dt) {
        var entity = agent.Entity;
        if (!entity.TryGet<MovementComponent>(out var movement)
            || !entity.TryGet<InputComponent>(out var input)) {
            return;
        }

        var steering = movement.Steering;
        var inputDirection = input.Direction;
        if (inputDirection.LengthSquared() > 1f) {
            inputDirection = Vector2.Normalize(inputDirection);
        }

        steering.InputDirection = inputDirection;

        RunBehaviors(agent, ref movement, dt);

        agent.Entity.PreviousPosition = agent.Entity.Position;
        agent.Entity.Position += movement.Velocity * dt;
        entity.Set(movement);
    }

    /// <summary>
    ///     Applies all behaviors to the given agent.
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="movement"></param>
    /// <param name="dt"></param>
    private void RunBehaviors(SteeringAgent agent, ref MovementComponent movement, float dt) {
        var steering = movement.Steering;
        if (steering.HasDash || steering.PunchTimer > 0f || steering.PunchFreezeTimer > 0f) {
            ApplySteeringVelocity(ref movement, Vector2.Zero, movement.Speed, dt);
            return;
        }

        var moveSpeed = steering.MoveSpeed > 0f ? steering.MoveSpeed : movement.Speed;
        var steer = Vector2.Zero;
        if (steering.InputDirection.LengthSquared() > 0f) {
            steer += steering.InputDirection * moveSpeed;
        }

        var steeringAgent = agent with { Movement = movement };
        foreach (var behavior in Behaviors)
            behavior.Apply(_context, steeringAgent, ref steer);

        ApplySteeringVelocity(ref movement, steer, moveSpeed, dt);
    }

    /// <summary>
    ///     Prepares the direction vector for the given agent.
    /// </summary>
    /// <param name="agentPosition"></param>
    /// <param name="targetPosition"></param>
    private void PrepareDirection(Vector2 agentPosition, Vector2 targetPosition) {
        var toTarget = targetPosition - agentPosition;
        var distanceSquared = toTarget.LengthSquared();
        if (distanceSquared <= SteeringConstants.MinDistanceSquared) {
            _context.HasDirection = false;
            return;
        }

        var inverseDistance = 1f / MathF.Sqrt(distanceSquared);
        _context.HasDirection = true;
        _context.Direction = toTarget * inverseDistance;
        _context.Distance = distanceSquared * inverseDistance;
    }

    /// <summary>
    ///     Determines the update interval for the given nearest player distance.
    /// </summary>
    /// <param name="nearestPlayerDistanceSquared"></param>
    /// <returns></returns>
    private static int ResolveUpdateInterval(float nearestPlayerDistanceSquared) {
        return nearestPlayerDistanceSquared switch {
            <= SteeringConstants.NearPlayerDistanceSquared => 1,
            <= SteeringConstants.MidPlayerDistanceSquared => 2,
            _ => 3
        };
    }

    /// <summary>
    ///     Applies the steering velocity to the given movement component.
    /// </summary>
    /// <param name="movement"></param>
    /// <param name="steer"></param>
    /// <param name="moveSpeed"></param>
    /// <param name="deltaTime"></param>
    private static void ApplySteeringVelocity(ref MovementComponent movement, Vector2 steer, float moveSpeed,
        float deltaTime) {
        var steering = movement.Steering;
        if (steering.HasDash) {
            steering.DashTimer -= deltaTime;

            if (steering.DashTimer <= 0f) {
                steering.HasDash = false;
                movement.Velocity.X = 0f;
                movement.Velocity.Z = 0f;
                return;
            }

            movement.Velocity.X = steering.DashDirection.X * steering.DashSpeed;
            movement.Velocity.Z = steering.DashDirection.Y * steering.DashSpeed;
            return;
        }

        if (steering.PunchTimer > 0f) {
            steering.PunchTimer -= deltaTime;

            movement.Velocity.X = steering.PunchDirection.X * steering.PunchOverrideSpeed;
            movement.Velocity.Z = steering.PunchDirection.Y * steering.PunchOverrideSpeed;
            return;
        }

        if (steering.PunchFreezeTimer > 0f) {
            steering.PunchFreezeTimer -= deltaTime;
            movement.Velocity.X = 0f;
            movement.Velocity.Z = 0f;
            return;
        }

        steering.HasPunch = false;
        steering.PunchOverrideSpeed = 0f;

        var steerLengthSquared = steer.LengthSquared();
        if (steerLengthSquared <= SteeringConstants.MinDistanceSquared) {
            movement.Velocity.X = 0f;
            movement.Velocity.Z = 0f;
            return;
        }

        var steerLength = MathF.Sqrt(steerLengthSquared);
        var direction = steer / steerLength;
        var speedFactor = MathF.Min(1f, steerLength);
        var speed = moveSpeed * speedFactor;

        movement.Velocity.X = direction.X * speed;
        movement.Velocity.Z = direction.Y * speed;
    }

    /// <summary>
    ///     Returns the flat position of the given vector.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private static Vector2 Flat(Vector3 position) {
        return new Vector2(position.X, position.Z);
    }
}