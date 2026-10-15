using Yggdrasilnet.Server.Event.Types;

namespace Yggdrasilnet.Server.Event.Events.Server;

/// <summary>
///  Event triggered when the server stops.
/// </summary>
public class ServerStopEvent : ServerEvent {
    public ServerStopEvent(Bootstrap.Server server) : base(server) { }
}