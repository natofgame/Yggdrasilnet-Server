using Yggdrasilnet.NetworkV4.Packets;

namespace Yggdrasilnet.Server.Network.Handler;

public interface IGamePacketHandler {
    void Handle(RequestProtocolPacket packet);
    void Handle(InputPacket packet);
    void Handle(CastSpellPacket packet);
    void Handle(ProtocolAcceptedPacket packet);
    void Handle(CommandPacket packet);
}
