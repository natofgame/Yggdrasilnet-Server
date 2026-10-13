namespace Yggdrasilnet.Server.Core.Events.Events;

public sealed class SpellCastRequestEvent : EntityEvent {
    public SpellCastRequestEvent(int entityId, int slotIndex) : base(entityId) {
        SlotIndex = slotIndex;
    }

    public int SlotIndex { get; }
}
