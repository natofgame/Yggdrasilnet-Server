using System.Text;
using Serilog;
using Yggdrasilnet.Network.Chat;
using Yggdrasilnet.Network.Packets;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;

namespace Yggdrasilnet.Server.Core.Services;

/// <summary>
///     Service for the chat: player messages and system messages (command feedback, announcements).
/// </summary>
/// <param name="network"></param>
/// <param name="sessions"></param>
public sealed class ChatService(NetworkService network, SessionRegistry sessions) {
    /// <summary>
    ///     Entity id used for messages that do not come from a player.
    /// </summary>
    public const int SystemSenderId = -1;

    /// <summary>
    ///     Broadcasts a message written by a player to every player in game.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void Broadcast(GameSession sender, string? message) {
        if (sender.Profile is null || !sender.TryGetEntityBinding(out var entityId, out _)) {
            return;
        }

        var text = FormatPlayerMessage(sender.Profile.Name, message);
        if (text is null) {
            return;
        }

        Log.Information("[Chat] {Name}: {Message}", sender.Profile.Name, ChatFormatting.StripCodes(text));
        SendToAll(new ChatPacket { EntityId = entityId, Message = text });
    }

    /// <summary>
    ///     Sends a system message to one player.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="text"></param>
    public void SendSystemMessage(GameSession session, string text) {
        network.Send(session, new ChatPacket { EntityId = SystemSenderId, Message = text });
    }

    /// <summary>
    ///     Sends a system message to every player in game.
    /// </summary>
    /// <param name="text"></param>
    public void BroadcastSystemMessage(string text) {
        Log.Information("[Chat] {Message}", ChatFormatting.StripCodes(text));
        SendToAll(new ChatPacket { EntityId = SystemSenderId, Message = text });
    }

    /// <summary>
    ///     Cleans a player message and formats it as <c>name: message</c>.
    ///     Returns null when there is nothing to send.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static string? FormatPlayerMessage(string name, string? message) {
        var clean = Sanitize(message);
        if (clean.Length == 0) {
            return null;
        }

        return $"{ChatFormatting.Gray}{ChatFormatting.Escape(name)}{ChatFormatting.White}: {ChatFormatting.Escape(clean)}";
    }

    /// <summary>
    ///     Removes control characters, trims and caps the length.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static string Sanitize(string? message) {
        if (string.IsNullOrEmpty(message)) {
            return string.Empty;
        }

        var builder = new StringBuilder(message.Length);
        foreach (var c in message) {
            if (!char.IsControl(c)) {
                builder.Append(c);
            }
        }

        var clean = builder.ToString().Trim();
        return clean.Length > ChatFormatting.MaxMessageLength
            ? clean[..ChatFormatting.MaxMessageLength].TrimEnd()
            : clean;
    }

    private void SendToAll(ChatPacket packet) {
        foreach (var session in sessions.All) {
            if (session.Phase == ConnectionPhase.Game) {
                network.Send(session, packet);
            }
        }
    }
}
