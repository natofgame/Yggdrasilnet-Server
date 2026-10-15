namespace Yggdrasilnet.Server.Core.Events;

public abstract class WorldEvent {
    public WorldEventContext Context { get; internal set; } = null!;
}