using LiteNetLib;

namespace Yggdrasilnet.Server.Network.Session;

public sealed class SessionRegistry {
    private readonly Dictionary<NetPeer, GameSession> _sessions = new();

    public IReadOnlyCollection<GameSession> All => _sessions.Values;

    public void Add(GameSession session) {
        _sessions[session.Peer] = session;
    }

    public bool Remove(NetPeer peer, out GameSession? session) {
        return _sessions.Remove(peer, out session);
    }

    public bool TryGet(NetPeer peer, out GameSession? session) {
        return _sessions.TryGetValue(peer, out session);
    }
}
