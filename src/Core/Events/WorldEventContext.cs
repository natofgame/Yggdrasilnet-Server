using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.Events;

public delegate bool EntityResolver(int entityId, out Entity? entity);

public sealed class WorldEventContext {
    private readonly EntityResolver _entityResolver;
    private readonly Action<string> _log;

    public WorldEventContext(
        string serverName,
        string serverVersion,
        IWorldEventBus events,
        EntityResolver entityResolver,
        Action<string> log) {
        ServerName = serverName;
        ServerVersion = serverVersion;
        Events = events;
        _entityResolver = entityResolver;
        _log = log;
    }

    public string ServerName { get; }
    public string ServerVersion { get; }
    public IWorldEventBus Events { get; }

    public void Log(string message) {
        _log(message);
    }

    public bool TryResolveEntity(int entityId, out Entity? entity) {
        entity = null;
        return _entityResolver(entityId, out entity);
    }
}
