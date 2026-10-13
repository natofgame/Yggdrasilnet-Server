using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components.Combat;

namespace Yggdrasilnet.Server.Core.Behavior.Impl;

/// <summary>
///     Authoring JSON du CombatComponent : liste des sorts assignes par slot
///     (ex: "spells": ["fireball", "", "dash"]). Le runtime (cooldown/charges/stacks) part
///     toujours a zero/defaut ; l'initialisation des charges (StartFull) est faite
///     paresseusement par SpellCastSystem au premier tick, car elle depend du behavior
///     "charges" du sort (non resolu ici).
/// </summary>
public sealed class CombatComponentBehavior : ComponentBehavior<CombatComponent> {
    public string[] Spells { get; set; } = [];

    protected override CombatComponent GetComponent(Vector3 spawnPosition) {
        var slots = new CombatSlot[Spells.Length];
        for (var i = 0; i < Spells.Length; i++)
            slots[i] = new CombatSlot { SpellId = Spells[i] ?? string.Empty };

        return new CombatComponent {
            Slots = slots,
            CastSlot = -1,
            CastSpellId = string.Empty
        };
    }
}
