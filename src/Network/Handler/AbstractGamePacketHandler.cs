using Yggdrasilnet.NetworkV4.Packets;
using Yggdrasilnet.NetworkV4.Packets.Login;

namespace Yggdrasilnet.Server.Network.Handler;

/// <summary>
///     Abstract implementation of <see cref="IGamePacketHandler" />.
/// </summary>
public abstract class AbstractGamePacketHandler : IGamePacketHandler {
    /// <inheritdoc />
    public virtual void Handle(RequestProtocolPacket packet) { }

    /// <inheritdoc />
    public virtual void Handle(InputPacket packet) { }

    /// <inheritdoc />
    public virtual void Handle(CastSpellPacket packet) { }

    /// <inheritdoc />
    public virtual void Handle(ProtocolAcceptedPacket packet) { }

    /// <inheritdoc />
    public virtual void Handle(CommandPacket packet) { }
    
    /// <inheritdoc />
    public virtual void Handle(ChatPacket packet) { }
    
    /// <inheritdoc />
    public virtual void Handle(LoginPacket packet) { }

    /// <inheritdoc />
    public virtual void Handle(CommandSuggestionRequestPacket packet) { }
}