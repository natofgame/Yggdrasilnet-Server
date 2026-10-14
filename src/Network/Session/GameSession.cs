using LiteNetLib;
using Yggdrasilnet.NetworkV4.Codec;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Network.Handler;

namespace Yggdrasilnet.Server.Network.Session;

/// <summary>
///  Represents a session between a client and the server.
/// </summary>
/// <param name="peer"></param>
/// <param name="protocol"></param>
public sealed class GameSession(NetPeer peer, GameProtocol protocol) {
    private const string DefaultDimensionName = "overworld";
    private bool _hasSequence;

    /// <summary>
    ///  The peer associated with this session.
    /// </summary>
    public NetPeer Peer { get; } = peer;
    
    /// <summary>
    ///  The packet codec for this session.
    /// </summary>
    public PacketCodec Codec { get; } = new(protocol);
    
    /// <summary>
    ///  The packet handler for this session.
    /// </summary>
    public IGamePacketHandler? Handler { get; private set; }
    
    /// <summary>
    ///  The phase of the connection.
    /// </summary>
    public ConnectionPhase Phase { get; private set; } = ConnectionPhase.Handshake;

    /// <summary>
    ///  The last sequence number received.
    /// </summary>
    public uint LastSequence { get; private set; }
    
    /// <summary>
    ///  The version of the snapshot definitions.
    /// </summary>
    public int SnapshotDefinitionsVersion { get; set; }
    
    /// <summary>
    ///  Snapshot of the known entities.
    /// </summary>
    public HashSet<int> SnapshotKnownEntities { get; } = [];
    
    /// <summary>
    ///  The entity the session is controlling.
    /// </summary>
    public int? ControlledEntityId { get; private set; }
    
    /// <summary>
    ///  The dimension the entity is in.
    /// </summary>
    public string DimensionName { get; private set; } = DefaultDimensionName;

    /// <summary>
    ///  Sets the packet handler for this session.
    /// </summary>
    /// <param name="handler"></param>
    public void SetHandler(IGamePacketHandler handler) {
        Handler = handler;
    }

    /// <summary>
    ///  Sets the protocol to use for this session.
    /// </summary>
    /// <param name="protocolToUse"></param>
    public void SetProtocol(GameProtocol protocolToUse) {
        Codec.SetProtocol(protocolToUse);
    }

    /// <summary>
    /// Marks the session as ready to play.
    /// </summary>
    public void MarkReady() {
        Phase = ConnectionPhase.Game;
    }

    /// <summary>
    ///  Binds the session to an entity.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="dimensionName"></param>
    public void BindEntity(int entityId, string dimensionName) {
        ControlledEntityId = entityId;
        DimensionName = string.IsNullOrWhiteSpace(dimensionName)
            ? DefaultDimensionName
            : dimensionName;
    }

    /// <summary>
    ///  Clears the entity binding.
    /// </summary>
    public void ClearEntityBinding() {
        ControlledEntityId = null;
        DimensionName = DefaultDimensionName;
    }

    /// <summary>
    /// Tries to get the entity binding.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="dimensionName"></param>
    /// <returns></returns>
    public bool TryGetEntityBinding(out int entityId, out string dimensionName) {
        if (ControlledEntityId is null) {
            entityId = 0;
            dimensionName = DefaultDimensionName;
            return false;
        }

        entityId = ControlledEntityId.Value;
        dimensionName = DimensionName;
        return true;
    }

    /// <summary>
    ///    Applies the input to the controlled entity.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    public bool ApplyInput(float x, float z, uint sequence) {
        if (_hasSequence && !IsNewer(sequence, LastSequence)) {
            return false;
        }

        LastSequence = sequence;
        _hasSequence = true;
        return true;
    }

    /// <summary>
    ///  Returns true if the candidate is newer than the current.
    /// </summary>
    /// <param name="candidate"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    private static bool IsNewer(uint candidate, uint current) {
        var diff = candidate - current;
        return diff != 0 && diff < 0x80000000;
    }
}
