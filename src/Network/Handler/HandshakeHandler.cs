using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Core.Services;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Network.Handler;

/// <summary>
///     Handles the handshake.
/// </summary>
/// <param name="runtime"></param>
/// <param name="session"></param>
/// <param name="network"></param>
/// <param name="playerService"></param>
public sealed class HandshakeHandler(
    Simulation runtime,
    GameSession session,
    NetworkService network,
    PlayerService playerService) : AbstractGamePacketHandler {
    
    /// <inheritdoc />
    public override void Handle(RequestProtocolPacket packet) {
        if (!network.TrySelectProtocol(session, packet.ProtocolVersion)) {
            Log.Warning("Unsupported protocol {ProtocolVersion} from {EndPoint}",
                packet.ProtocolVersion, session.Peer.Address);
            network.Disconnect(session);
            return;
        }

        network.Send(session, new ProtocolAcceptedPacket {
            ProtocolVersion = session.Codec.Protocol.ProtocolVersion
        });

        session.SetPhase(ConnectionPhase.Login);
        session.SetHandler(new LoginHandler(runtime, session, network, playerService));
    }
    
    /// <inheritdoc />
    public override void Handle(InputPacket packet) {
        Log.Debug("Input ignored for {EndPoint} because handshake is not completed.", session.Peer.Address);
    }

    /// <inheritdoc />
    public override void Handle(CastSpellPacket packet) {
        Log.Debug("CastSpell ignored for {EndPoint} because handshake is not completed.", session.Peer.Address);
    }
}