using Serilog;
using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.NetworkV4.Packets.Command;
using Yggdrasilnet.NetworkV4.Packets.Protocol;
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
        Log.Information("[{EndPoint}] issued command: {Command}", session.Peer.Address, packet.Command);
        simulation.CommandFactory.Execute(packet.Command, CreateCommandSource());
    }

    /// <inheritdoc />
    public override void Handle(ChatPacket packet) {
        simulation.Chat.Broadcast(session, packet.Message);
    }

    /// <inheritdoc />
    public override void Handle(CommandSuggestionRequestPacket packet) {
        var text = packet.Text;
        var offset = text.StartsWith('/') ? 1 : 0;
        var suggestions = simulation.CommandFactory.Suggest(text[offset..], CreateCommandSource());

        var response = new CommandSuggestionResponsePacket {
            RequestId = packet.RequestId,
            Start = suggestions.Start + offset,
            Usage = suggestions.Usage
        };
        response.Suggestions.AddRange(suggestions.Suggestions);

        simulation.Network.Send(session, response);
    }

    private CommandSource CreateCommandSource() {
        session.TryGetEntityBinding(out var entityId, out var dimensionName);

        return new CommandSource {
            ExecutorEntityId = entityId,
            Dimension = simulation.World.GetDimension(dimensionName),
            PermissionLevel = 0,
            SendFeedback = msg => simulation.Chat.SendSystemMessage(session, msg)
        };
    }
}