using LiteNetLib;

namespace Yggdrasilnet.Server.Network.Session;

/// <summary>
///  Registry of all sessions.
/// </summary>
public sealed class SessionRegistry {
    private readonly Dictionary<NetPeer, GameSession> _sessions = new();

    /// <summary>
    ///  All sessions.
    /// </summary>
    public IReadOnlyCollection<GameSession> All => _sessions.Values;

    /// <summary>
    ///  Adds a session.
    /// </summary>
    /// <param name="session"></param>
    public void Add(GameSession session) {
        _sessions[session.Peer] = session;
    }

    /// <summary>
    ///  Removes a session by peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="session"></param>
    /// <returns></returns>
    public bool Remove(NetPeer peer, out GameSession? session) {
        return _sessions.Remove(peer, out session);
    }

    /// <summary>
    ///  Tries to get a session by peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="session"></param>
    /// <returns></returns>
    public bool TryGet(NetPeer peer, out GameSession? session) {
        return _sessions.TryGetValue(peer, out session);
    }
}
