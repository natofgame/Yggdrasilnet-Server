using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.World.Entities.Components.Combat;

namespace Yggdrasilnet.Server.Core.Behavior.Behaviors.Entity;

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
