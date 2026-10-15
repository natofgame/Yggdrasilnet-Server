using Yggdrasilnet.Server.Event.Types;

namespace Yggdrasilnet.Server.Event.Events.Server;

/// <summary>
///  Event triggered when the server starts.
/// </summary>
public class ServerStartEvent : ServerEvent {
    public ServerStartEvent(Bootstrap.Server server) : base(server) { }
}