using LiteNetLib;
using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Network.Handler;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Network.Services;

public sealed class NetworkService(ServerTransport server) {
    public bool TrySelectProtocol(GameSession session, int version) {
        if (!ProtocolRegistry.TryGetProtocol(version, out var protocol)) {
            return false;
        }

        session.SetProtocol(protocol);
        return true;
    }

    public void Send(GameSession session, IGamePacket packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) {
        if (!server.Send(session.Peer, packet, session.Codec, method)) {
            Log.Warning("Unable to send {PacketType} to {EndPoint}",
                packet.GetType().Name, session.Peer.Address);
        }
    }

    public void Disconnect(GameSession session) {
        session.Peer.Disconnect();
    }

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
