namespace Yggdrasilnet.Server.Core.Events.Events;

public sealed class PlayerDeconnexionEvent : EntityEvent {
    public PlayerDeconnexionEvent(string endPoint, int entityId, string reason)
        : base(entityId) {
        EndPoint = endPoint;
        Reason = reason;
    }

    public string EndPoint { get; }
    public string Reason { get; }
}
