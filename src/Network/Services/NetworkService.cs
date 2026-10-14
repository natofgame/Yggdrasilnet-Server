using LiteNetLib;
using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Network.Handler;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Network.Services;

/// <summary>
///  Network service.
/// </summary>
/// <param name="server"></param>
public sealed class NetworkService(ServerTransport server) {
    /// <summary>
    ///  Tries to select a protocol for the given session.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public bool TrySelectProtocol(GameSession session, int version) {
        if (!ProtocolRegistry.TryGetProtocol(version, out var protocol)) {
            return false;
        }

        session.SetProtocol(protocol);
        return true;
    }

    /// <summary>
    ///  Sends a packet to the given session.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    /// <param name="method"></param>
    public void Send(GameSession session, IGamePacket packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) {
        if (!server.Send(session.Peer, packet, session.Codec, method)) {
            Log.Warning("Unable to send {PacketType} to {EndPoint}",
                packet.GetType().Name, session.Peer.Address);
        }
    }
    
    /// <summary>
    ///  Disconnects the given session.
    /// </summary>
    /// <param name="session"></param>
    public void Disconnect(GameSession session) {
        session.Peer.Disconnect();
    }

    /// <summary>
    ///  Dispatches a packet to the given handler.
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="packet"></param>
    public void DispatchPacket(IGamePacketHandler handler, IGamePacket packet) {
        switch (packet) {
            case RequestProtocolPacket requestProtocol:
                handler.Handle(requestProtocol);
                break;
            case InputPacket input:
                handler.Handle(input);
                break;
            case CastSpellPacket castSpell:
                handler.Handle(castSpell);
                break;
            case ProtocolAcceptedPacket protocolAccepted:
                handler.Handle(protocolAccepted);
                break;
            case CommandPacket command:
                handler.Handle(command);
                break;
        }
    }
}
