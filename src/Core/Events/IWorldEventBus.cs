namespace Yggdrasilnet.Server.Core.Events;

public interface IWorldEventBus {
    void Register(object listener);
    void Unregister(object listener);
    void Post(WorldEvent worldEvent);
}
