using LiteNetLib;

namespace Yggdrasilnet.Server.Network;

/// <summary>
///  Interface for server events.
/// </summary>
public interface IServerEvent;

/// <summary>
///  Event triggered when a peer connects.
/// </summary>
/// <param name="Peer"></param>
public sealed record PeerConnectedEvent(NetPeer Peer) : IServerEvent;

/// <summary>
///  Event triggered when a peer disconnects.
/// </summary>
/// <param name="Peer"></param>
/// <param name="Info"></param>
public sealed record PeerDisconnectedEvent(NetPeer Peer, DisconnectInfo Info) : IServerEvent;

/// <summary>
///  Event triggered when a packet is received.
/// </summary>
/// <param name="Peer"></param>
/// <param name="Payload"></param>
public sealed record PacketReceivedEvent(NetPeer Peer, byte[] Payload) : IServerEvent;

/// <summary>
///  Event triggered when a console command is received.
/// </summary>
/// <param name="Input"></param>
public sealed record ConsoleCommandEvent(string Input) : IServerEvent;
