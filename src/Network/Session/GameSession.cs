using System.Numerics;
using LiteNetLib;
using Yggdrasilnet.NetworkV4.Codec;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Network.Handler;

namespace Yggdrasilnet.Server.Network.Session;

public enum ConnectionPhase {
    Handshake,
    Game
}

public sealed class GameSession(NetPeer peer, GameProtocol protocol) {
    private const string DefaultDimensionName = "overworld";
    private bool _hasSequence;

    public NetPeer Peer { get; } = peer;
    public PacketCodec Codec { get; } = new(protocol);
    public IGamePacketHandler? Handler { get; private set; }
    public ConnectionPhase Phase { get; private set; } = ConnectionPhase.Handshake;

    public uint LastSequence { get; private set; }
    public int SnapshotDefinitionsVersion { get; set; }
    public HashSet<int> SnapshotKnownEntities { get; } = [];
    public Vector2 Input { get; private set; }
    public Vector2 Position { get; private set; }
    public int? ControlledEntityId { get; private set; }
    public string DimensionName { get; private set; } = DefaultDimensionName;

    public void SetHandler(IGamePacketHandler handler) {
        Handler = handler;
    }

    public void SetProtocol(GameProtocol protocolToUse) {
        Codec.SetProtocol(protocolToUse);
    }

    public void MarkReady() {
        Phase = ConnectionPhase.Game;
    }

    public void BindEntity(int entityId, string dimensionName) {
        ControlledEntityId = entityId;
        DimensionName = string.IsNullOrWhiteSpace(dimensionName)
            ? DefaultDimensionName
            : dimensionName;
    }

    public void ClearEntityBinding() {
        ControlledEntityId = null;
        DimensionName = DefaultDimensionName;
    }

    public bool TryGetEntityBinding(out int entityId, out string dimensionName) {
        if (ControlledEntityId is null) {
            entityId = default;
            dimensionName = DefaultDimensionName;
            return false;
        }

        entityId = ControlledEntityId.Value;
        dimensionName = DimensionName;
        return true;
    }

    public bool ApplyInput(float x, float z, uint sequence) {
        if (_hasSequence && !IsNewer(sequence, LastSequence)) {
            return false;
        }

        Input = new Vector2(x, z);
        LastSequence = sequence;
        _hasSequence = true;
        return true;
    }

    public void SyncPosition(float x, float z) {
        Position = new Vector2(x, z);
    }

    private static bool IsNewer(uint candidate, uint current) {
        var diff = candidate - current;
        return diff != 0 && diff < 0x80000000;
    }
}
