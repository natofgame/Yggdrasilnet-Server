using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.NetworkV4.Packets.Login;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Core.Services;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Network.Handler;

public sealed class LoginHandler (
    Simulation runtime,
    GameSession session,
    NetworkService network,
    PlayerService playerService) : AbstractGamePacketHandler {

    public override void Handle(LoginPacket packet) {
        if (!playerService.TryLogin(session, packet.Name, out var error)) {
            Log.Information("Login rejected for {EndPoint}: {Error}", session.Peer.Address, error);
            network.Send(session, new LoginResultPacket { Success = false, Reason = error });
            network.Disconnect(session);
            return;
        }

        network.Send(session, new LoginResultPacket {
            Success = true,
            PlayerId = session.Profile!.Id.ToString(),
            Name = session.Profile.Name
        });
        playerService.EnterGame(session);

        session.SetHandler(new GameHandler(runtime, session, playerService));
        Log.Information("{Name} logged in from {EndPoint}", session.Profile.Name, session.Peer.Address);
    }

    public override void Handle(InputPacket packet) =>
        Log.Debug("Input ignored for {EndPoint}: not logged in.", session.Peer.Address);

    public override void Handle(CastSpellPacket packet) =>
        Log.Debug("CastSpell ignored for {EndPoint}: not logged in.", session.Peer.Address);
}