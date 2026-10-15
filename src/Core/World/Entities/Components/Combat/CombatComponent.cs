using System.Numerics;
using Yggdrasilnet.NetworkV4.Enums;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Combat;

public struct CombatSlot {
    public string SpellId = string.Empty;
    public bool Initialized;
    public float CooldownRemaining;
    public int ChargesCurrent;
    public float ChargeRecoveryRemaining;
    public int Stacks;
    public float StackRemaining;

    public CombatSlot() { }
}

public class CombatComponent : IComponent {
    public Vector3 CastDirection;
    public bool CastHasDashEffect;
    public Vector3 CastOrigin;
    public SpellPhase CastPhase;
    public int CastSlot;
    public string CastSpellId = string.Empty;

    public SpellType CastSpellType;

    public bool IsCasting;
    public float PhaseDuration;
    public float PhaseTimer;
    public CombatSlot[] Slots = [];
}