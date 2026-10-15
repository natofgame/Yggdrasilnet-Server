using System.Numerics;
using System.Runtime.CompilerServices;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Living;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Ai;

/// <summary>
///     Handles the AI brain.
/// </summary>
public static class AiBrain {
    private const float MinDirectionSq = 0.0001f;
    private const float TransitionScoreMargin = 0.12f;

    private static readonly AiBehavior[] DefaultActions = [
        new FleeBehavior(),
        new AttackBehavior(),
        new RepositionBehavior(),
        new HoldBehavior(),
        new FlankBehavior(),
        new WanderBehavior()
    ];

    /// <summary>
    ///     Updates the AI's state.'
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="entity"></param>
    /// <param name="ai"></param>
    /// <param name="hp"></param>
    /// <param name="input"></param>
    /// <param name="movement"></param>
    /// <param name="dt"></param>
    public static void Tick(
        Dimension dimension,
        Entity entity,
        ref BrainComponent ai,
        in LivingComponent hp,
        ref InputComponent input,
        ref MovementComponent movement,
        float dt
    ) {
        if (hp.Max <= 0f) {
            AiMotionController.Stop(ref input, ref movement);
            return;
        }

        if (ai.AttackCooldown > 0f) {
            ai.AttackCooldown = MathF.Max(0f, ai.AttackCooldown - dt);
        }

        ai.HealthRatio = hp.Current / hp.Max;
        var hasTarget = RefreshTarget(dimension, entity, ref ai, dt);
        if (hasTarget && !ai.HadTargetLastTick) {
            ai.IsSurprised = true;
            ai.SurprisedTimer = ai.SurprisedDuration;
        }

        ai.HadTargetLastTick = hasTarget;

        if (ai.SurprisedTimer > 0f) {
            ai.SurprisedTimer = MathF.Max(0f, ai.SurprisedTimer - dt);
            ai.IsSurprised = ai.SurprisedTimer > 0f;
            AiMotionController.Stop(ref input, ref movement);
            return;
        }

        ai.IsSurprised = false;

        var actions = ai.Actions.Length > 0 ? ai.Actions : DefaultActions;
        var context = BuildContext(ai);
        var selected = SelectBehavior(actions, context, ref ai);
        if (selected is null) {
            AiMotionController.Stop(ref input, ref movement);
            return;
        }

        ai.CombatStateTimer += dt;
        if (ai.CombatStateLockTimer > 0f) {
            ai.CombatStateLockTimer = MathF.Max(0f, ai.CombatStateLockTimer - dt);
        }

        var intent = selected.BuildIntent(dimension, entity, ref ai, context, dt);
        
        // TODO: Implement
        /*if (intent.Type == AiIntentType.Cast && intent.SpellIndex >= 0) {
            dimension.World.Simulation.Server.Event.Publish(new SpellCastRequestEvent(entity.UniqueId, intent.SpellIndex));
        }*/

        AiMotionController.Apply(ref input, ref movement, intent);
    }

    /// <summary>
    ///     Selects the best behavior from the given actions.
    /// </summary>
    /// <param name="actions"></param>
    /// <param name="context"></param>
    /// <param name="ai"></param>
    /// <returns></returns>
    private static AiBehavior? SelectBehavior(AiBehavior[] actions, in AiContext context, ref BrainComponent ai) {
        AiBehavior? bestOverall = null;
        var bestOverallScore = 0f;
        AiBehavior? currentStateBest = null;
        var currentStateScore = 0f;

        foreach (var behavior in actions) {
            var score = behavior.Evaluate(context, ai);
            if (score <= 0f) {
                continue;
            }

            if (score > bestOverallScore) {
                bestOverall = behavior;
                bestOverallScore = score;
            }

            if (behavior.State != ai.CombatState || !(score > currentStateScore)) {
                continue;
            }

            currentStateBest = behavior;
            currentStateScore = score;
        }

        if (bestOverall is null) {
            return null;
        }

        var selected = bestOverall;
        if (selected.State != ai.CombatState
            && ai.CombatStateLockTimer > 0f
            && selected.State != AiCombatState.Retreat
            && currentStateBest is not null
            && bestOverallScore < currentStateScore + TransitionScoreMargin) {
            selected = currentStateBest;
        }

        if (selected.State == ai.CombatState) {
            return selected;
        }

        ai.CombatState = selected.State;
        ai.CombatStateTimer = 0f;
        ai.CombatStateLockTimer = MathF.Max(0.05f, selected.TransitionLockDuration);

        return selected;
    }

    /// <summary>
    ///     Builds the context for the AI.
    /// </summary>
    /// <param name="ai"></param>
    /// <returns></returns>
    private static AiContext BuildContext(in BrainComponent ai) {
        var targetDir = new Vector2(ai.TargetDirection.X, ai.TargetDirection.Z);
        targetDir = targetDir.LengthSquared() > MinDirectionSq ? Vector2.Normalize(targetDir) : Vector2.Zero;

        return new AiContext {
            HasTarget = ai.HasTarget,
            TargetDistance = ai.TargetDistance,
            TargetDirection = targetDir,
            HealthRatio = ai.HealthRatio,
            TargetHealthRatio = ai.TargetHealthRatio,
            HasAttackToken = ai.HasAttackToken,
            AttackCooldown = ai.AttackCooldown,
            AlliesNearby = ai.AlliesNearby,
            Prudence01 = Clamp01(ai.Prudence / 10f),
            Courage01 = Clamp01(ai.Courage / 10f),
            Aggressivity01 = Clamp01(ai.Aggressivity / 10f),
            Impulsivity01 = Clamp01(ai.Impulsivity / 10f),
            Curiosity01 = Clamp01(ai.Curiosity / 10f)
        };
    }

    /// <summary>
    ///     Refreshes the AI's target.'
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="self"></param>
    /// <param name="ai"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    private static bool RefreshTarget(Dimension dimension, Entity self, ref BrainComponent ai, float dt) {
        if (TryKeepCurrentTarget(dimension, self, ref ai)) {
            if (ai.PerceptionTimer > 0f) {
                ai.PerceptionTimer -= dt;
            }

            if (ai.PerceptionTimer <= 0f) {
                ai.PerceptionTimer = ai.PerceptionInterval;
            }

            return true;
        }

        ai.PerceptionTimer -= dt;
        if (ai.TargetEntityId is not null || ai.PerceptionTimer <= 0f) {
            ai.PerceptionTimer = ai.PerceptionInterval;
            if (TryFindNearestPlayer(dimension, self, ai.SightRange, out var target)) {
                ApplyTarget(dimension, self, ref ai, target, target.Position);
                return true;
            }
        }

        ClearTarget(ref ai);
        return false;
    }

    /// <summary>
    ///     Applies the current target to the AI.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="self"></param>
    /// <param name="ai"></param>
    /// <returns></returns>
    private static bool TryKeepCurrentTarget(Dimension dimension, Entity self, ref BrainComponent ai) {
        if (ai.TargetEntityId is not { } targetId || !dimension.TryGetEntity(targetId, out var target)) {
            return false;
        }

        if (!target.TryGet<CollisionComponent>(out var collider)
            || collider.Layer != CollisionLayer.Player) {
            return false;
        }

        if (target.TryGet<LivingComponent>(out var living) && !living.IsAlive) {
            return false;
        }

        ApplyTarget(dimension, self, ref ai, target, target.Position);
        return ai.TargetDistance <= ai.SightRange;
    }

    /// <summary>
    ///     Tries to find the nearest player.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="self"></param>
    /// <param name="range"></param>
    /// <param name="nearest"></param>
    /// <returns></returns>
    private static bool TryFindNearestPlayer(Dimension dimension, Entity self, float range, out Entity nearest) {
        nearest = null!;
        var bestDistSq = range * range;
        var found = false;

        foreach (var other in dimension.Query<CollisionComponent>()) {
            var collider = other.Get<CollisionComponent>();
            if (other.UniqueId == self.UniqueId || collider.Layer != CollisionLayer.Player) {
                continue;
            }

            if (other.TryGet<LivingComponent>(out var living) && !living.IsAlive) {
                continue;
            }

            var distSq = Vector3.DistanceSquared(self.Position, other.Position);
            if (distSq > bestDistSq) {
                continue;
            }

            bestDistSq = distSq;
            nearest = other;
            found = true;
        }

        return found;
    }

    /// <summary>
    ///     Applies the target to the AI.
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="self"></param>
    /// <param name="ai"></param>
    /// <param name="target"></param>
    /// <param name="targetPosition"></param>
    private static void ApplyTarget(
        Dimension dimension,
        Entity self,
        ref BrainComponent ai,
        Entity target,
        Vector3 targetPosition
    ) {
        var uniqueId = target.UniqueId;
        ai.TargetEntityId = uniqueId;
        ai.TargetHealthRatio = 0f;

        if (!dimension.TryGetEntity(uniqueId, out var targetEntity)) {
            return;
        }

        if (targetEntity.TryGet<LivingComponent>(out var living) && living.Max > 0f) {
            ai.TargetHealthRatio = living.Current / living.Max;
        }

        var toTarget = targetPosition - self.Position;
        toTarget.Y = 0f;
        ai.TargetDistance = toTarget.Length();
        ai.TargetDirection = toTarget.LengthSquared() > MinDirectionSq
            ? Vector3.Normalize(toTarget)
            : Vector3.Zero;
    }

    /// <summary>
    ///     Clears the AI's target.'
    /// </summary>
    /// <param name="ai"></param>
    private static void ClearTarget(ref BrainComponent ai) {
        ai.TargetEntityId = null;
        ai.TargetDistance = 0f;
        ai.TargetDirection = Vector3.Zero;
        ai.TargetHealthRatio = 0f;
        ai.HasAttackToken = false;
        ai.TokenTimer = 0f;
        ai.TokenWait = 0f;
        ai.SlotAngleOffset = 0f;
        ai.RepositionBurstTimer = 0f;
        ai.RepositionPulseTimer = 0f;
        ai.CombatState = AiCombatState.Idle;
        ai.CombatStateTimer = 0f;
        ai.CombatStateLockTimer = 0f;
    }

    /// <summary>
    ///     Clamps a value to [0, 1].
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Clamp01(float v) {
        return Math.Clamp(v, 0f, 1f);
    }
}