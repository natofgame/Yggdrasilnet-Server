using System.Numerics;
using Yggdrasilnet.Server.Core.Components;
using Yggdrasilnet.Server.Core.Systems.Ai;

namespace Yggdrasilnet.Server.Core.World.Entities.Components;

public class BrainComponent : IComponent {
    public AiBehavior[] Actions = [];
    public float Aggressivity = 0;
    public int AlliesNearby = 0;

    public float AttackCooldown = 0;
    public AiCombatState CombatState = AiCombatState.Idle;
    public float CombatStateLockTimer = 0;
    public float CombatStateTimer = 0;
    public float Courage = 0;
    public float Curiosity = 0;
    public float Discipline = 0;
    public float FearDuration = 2.5f;
    public float FearRadius = 8f;

    public bool HadTargetLastTick = false;
    public bool HasAttackToken = false;

    public float HealthRatio = 0;
    public float HoldOrbitDirection = 0;
    public float HoldOrbitSwitchTimer = 0;
    public float HoldRadiusOffset = 0;
    public float Impulsivity = 0;
    public bool IsSurprised = false;
    public float PerceptionInterval = 0.2f;
    public float PerceptionTimer = 0;
    public float Prudence = 0;
    public float RepositionBurstTimer = 0;
    public float RepositionPulseTimer = 0;

    public float SightRange = 20f;
    public float SlotAngle = 0;
    public float SlotAngleOffset = 0;
    public float SlotSwapTimer = 0;

    public float SurprisedDuration = 0.4f;
    public float SurprisedTimer = 0;
    public Vector3 TargetDirection = default;
    public float TargetDistance = 0;
    public int? TargetEntityId = null;
    public float TargetHealthRatio = 0;
    public float TokenTimer = 0;
    public float TokenWait = 0;
    public float WanderIdleTimer = 0;
    public Vector3? WanderTarget = null;
    public bool HasTarget => TargetEntityId.HasValue;
}