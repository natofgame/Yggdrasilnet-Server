using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using Yggdrasilnet.Network.Codec;
using Yggdrasilnet.Network.Protocol;

namespace Yggdrasilnet.Server.Network;

/// <summary>
///     Transport for the server.
/// </summary>
public sealed class ServerTransport() : INetEventListener {
    private readonly ConcurrentQueue<IServerEvent> _events = null!;
    private readonly NetManager _netManager = null!;
    private readonly NetDataWriter _writer = new();

    public ServerTransport(ConcurrentQueue<IServerEvent> events) : this() {
        _events = events;
        _netManager = new NetManager(this) { AutoRecycle = true };
    }

    /// <inheritdoc />
    public void OnPeerConnected(NetPeer peer) {
        _events.Enqueue(new PeerConnectedEvent(peer));
    }

    /// <inheritdoc />
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        _events.Enqueue(new PeerDisconnectedEvent(peer, disconnectInfo));
    }

    /// <inheritdoc />
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
        Log.Warning("Network error from {EndPoint}: {Error}", endPoint, socketError);
    }

    /// <inheritdoc />
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
        DeliveryMethod deliveryMethod) {
        _events.Enqueue(new PacketReceivedEvent(peer, reader.GetRemainingBytes()));
        reader.Recycle();
    }

    /// <inheritdoc />
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType) { }

    /// <inheritdoc />
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    /// <inheritdoc />
    public void OnConnectionRequest(ConnectionRequest request) {
        request.AcceptIfKey("Yggdrasilnet");
    }

    /// <summary>
    ///     Starts the server.
    /// </summary>
    /// <param name="port"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start(int port) {
        if (!_netManager.Start(port)) {
            throw new InvalidOperationException($"Unable to bind UDP server on port {port}.");
        }

        Log.Information("[Server] Listening on port {Port}", port);
    }

    /// <summary>
    ///     Stops the server.
    /// </summary>
    public void Stop() {
        _netManager.Stop();
    }

    /// <summary>
    ///     Polls for network events.
    /// </summary>
    public void Poll() {
        _netManager.PollEvents();
    }

    /// <summary>
    ///     Sends a packet to a peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="packet"></param>
    /// <param name="codec"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    public bool Send(NetPeer peer, IGamePacket packet, PacketCodec codec,
        DeliveryMethod method = DeliveryMethod.ReliableOrdered) {
        lock (_writer) {
            if (!codec.TryEncode(packet, _writer)) {
                return false;
            }

            peer.Send(_writer, method);
            return true;
        }
    }
}