namespace Yggdrasilnet.Server.Event.Types;

/// <summary>
///  Event triggered by the server.
/// </summary>
public class ServerEvent : IEvent {
    /// <summary>
    ///  The server that the event occurred in.
    /// </summary>
    public Bootstrap.Server Server { get; private set; }
    
    protected ServerEvent(Bootstrap.Server server) {
        Server = server;
    }
}