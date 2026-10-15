using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Core.Command;
using Yggdrasilnet.Server.Core.Services;
using Yggdrasilnet.Server.Core.Systems.Combat;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Network.Handler;

/// <summary>
///  Handles packets during the game phase.
/// </summary>
/// <param name="simulation"></param>
/// <param name="session"></param>
/// <param name="playerService"></param>
public sealed class GameHandler(
    Simulation simulation,
    GameSession session,
    PlayerService playerService) : AbstractGamePacketHandler {
    /// <inheritdoc />
    public override void Handle(RequestProtocolPacket packet) {
        Log.Debug("RequestProtocol ignored in game phase for {EndPoint}", session.Peer.Address);
        
    }

    /// <inheritdoc />
    public override void Handle(InputPacket packet) {
        playerService.ApplyInput(session, packet.MoveX, packet.MoveZ, packet.Sequence);
    }

    /// <inheritdoc />
    public override void Handle(CastSpellPacket packet) {
        if (!session.TryGetEntityBinding(out var entityId, out var dimensionName)) {
            Log.Debug("CastSpell from {EndPoint} ignored: no bound entity.", session.Peer.Address);
            return;
        }

        if (simulation.World.TryGetDimension(dimensionName, out var dimension)) {
            var spells = dimension.GetSystem<SpellCastSystem>();
            spells?.EnqueueCast(entityId, packet.SpellIndex);
        }
        
        Log.Debug("CastSpell from {EndPoint}: slot={SpellIndex}",
            session.Peer.Address, packet.SpellIndex);
    }

    /// <inheritdoc />
    public override void Handle(ProtocolAcceptedPacket packet) {
        Log.Debug("ProtocolAccepted received from {EndPoint} and ignored.", session.Peer.Address);
    }

    /// <inheritdoc />
    public override void Handle(CommandPacket packet) {
        session.TryGetEntityBinding(out var entityId, out var dimensionName);

        var source = new CommandSource {
            ExecutorEntityId = entityId,
            Dimension = simulation.World.GetDimension(dimensionName),
            PermissionLevel = 0,
            SendFeedback = msg => Log.Information("[{EndPoint}] {Message}", session.Peer.Address, msg)
        };

        simulation.CommandFactory.Execute(packet.Command, source);
    }
}