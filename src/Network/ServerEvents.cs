using LiteNetLib;

namespace Yggdrasilnet.Server.Network;

public interface IServerEvent;

public sealed record PeerConnectedEvent(NetPeer Peer) : IServerEvent;

public sealed record PeerDisconnectedEvent(NetPeer Peer, DisconnectInfo Info) : IServerEvent;

public sealed record PacketReceivedEvent(NetPeer Peer, byte[] Payload) : IServerEvent;

public sealed record ConsoleCommandEvent(string Input) : IServerEvent;
