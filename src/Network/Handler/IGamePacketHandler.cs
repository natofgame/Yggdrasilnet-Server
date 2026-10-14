using Yggdrasilnet.NetworkV4.Packets;

namespace Yggdrasilnet.Server.Network.Handler;

/// <summary>
///  Interface for handling game packets.
/// </summary>
public interface IGamePacketHandler {
    /// <summary>
    ///  Handles a request protocol packet.
    /// </summary>
    /// <param name="packet"></param>
    void Handle(RequestProtocolPacket packet);
    
    /// <summary>
    ///  Handles an input packet.
    /// </summary>
    /// <param name="packet"></param>
    void Handle(InputPacket packet);
    
    /// <summary>
    ///  Handles a cast spell packet.
    /// </summary>
    /// <param name="packet"></param>
    void Handle(CastSpellPacket packet);
    
    /// <summary>
    ///   Handles a protocol accepted packet.
    /// </summary>
    /// <param name="packet"></param>
    void Handle(ProtocolAcceptedPacket packet);
    
    /// <summary>
    ///  Handles a command packet.
    /// </summary>
    /// <param name="packet"></param>
    void Handle(CommandPacket packet);
}
