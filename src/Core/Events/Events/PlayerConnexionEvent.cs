namespace Yggdrasilnet.Server.Core.Events.Events;

public sealed class PlayerConnexionEvent : EntityEvent {
    public PlayerConnexionEvent(string endPoint, int entityId)
        : base(entityId) {
        EndPoint = endPoint;
    }

    public string EndPoint { get; }
}