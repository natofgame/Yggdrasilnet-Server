using Yggdrasilnet.NetworkV4.Packets;

namespace Yggdrasilnet.Server.Network.Handler;

public abstract class AbstractGamePacketHandler : IGamePacketHandler {
    public virtual void Handle(RequestProtocolPacket packet) { }
    public virtual void Handle(InputPacket packet) { }
    public virtual void Handle(CastSpellPacket packet) { }
    public virtual void Handle(ProtocolAcceptedPacket packet) { }
    public virtual void Handle(CommandPacket packet) { }
}
