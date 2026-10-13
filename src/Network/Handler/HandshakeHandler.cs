using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;
using Yggdrasilnet.Server.Runtime;
using Yggdrasilnet.Server.Runtime.Services;

namespace Yggdrasilnet.Server.Network.Handler;

public sealed class HandshakeHandler(
    SimulationEngine runtime,
    GameSession session,
    NetworkService network,
    PlayerService playerService) : AbstractGamePacketHandler {
    public override void Handle(RequestProtocolPacket packet) {
        if (!network.TrySelectProtocol(session, packet.ProtocolVersion)) {
            Log.Warning("Unsupported protocol {ProtocolVersion} from {EndPoint}", packet.ProtocolVersion,
                session.Peer.Address);
            network.Disconnect(session);
            return;
        }

        network.Send(session, new ProtocolAcceptedPacket {
            ProtocolVersion = session.Codec.Protocol.ProtocolVersion
        });

        playerService.EnterGame(session);
        session.SetHandler(new GameHandler(runtime, session, playerService));
        Log.Information("Handshake completed with {EndPoint} using protocol v{ProtocolVersion}",
            session.Peer.Address, session.Codec.Protocol.ProtocolVersion);
    }

    public override void Handle(InputPacket packet) {
        Log.Debug("Input ignored for {EndPoint} because handshake is not completed.", session.Peer.Address);
    }

    public override void Handle(CastSpellPacket packet) {
        Log.Debug("CastSpell ignored for {EndPoint} because handshake is not completed.", session.Peer.Address);
    }
}
