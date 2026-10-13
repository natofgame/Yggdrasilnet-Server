using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using Yggdrasilnet.NetworkV4.Codec;
using Yggdrasilnet.NetworkV4.Protocol;

namespace Yggdrasilnet.Server.Network;

public sealed class ServerTransport() : INetEventListener {
    private readonly NetManager _netManager = null!;
    private readonly NetDataWriter _writer = new();
    public readonly ConcurrentQueue<IServerEvent> Events = null!;

    public ServerTransport(ConcurrentQueue<IServerEvent> events) : this() {
        Events = events;
        _netManager = new NetManager(this) { AutoRecycle = true };
    }

    public void OnPeerConnected(NetPeer peer) {
        Events.Enqueue(new PeerConnectedEvent(peer));
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        Events.Enqueue(new PeerDisconnectedEvent(peer, disconnectInfo));
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
        Log.Warning("Network error from {EndPoint}: {Error}", endPoint, socketError);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
        DeliveryMethod deliveryMethod) {
        Events.Enqueue(new PacketReceivedEvent(peer, reader.GetRemainingBytes()));
        reader.Recycle();
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType) { }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    public void OnConnectionRequest(ConnectionRequest request) {
        request.AcceptIfKey("Yggdrasilnet");
    }

    public void Start(int port) {
        if (!_netManager.Start(port)) {
            throw new InvalidOperationException($"Unable to bind UDP server on port {port}.");
        }

        Log.Information("Server listening on port {Port}", port);
    }

    public void Stop() {
        _netManager.Stop();
    }

    public void Poll() {
        _netManager.PollEvents();
    }

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
