using System.Text.Json.Serialization;

namespace Yggdrasilnet.Server.Configuration;

public sealed class SnapshotConfiguration {
    /// <summary>
    ///  Target size of a snapshot chunk packet, header included. Should stay below the transport MTU.
    /// </summary>
    [JsonPropertyName("chunkTargetBytes")] public int ChunkTargetBytes { get; set; } = 1000;

    /// <summary>
    ///  Delay between two reliable keyframes.
    /// </summary>
    [JsonPropertyName("keyframeIntervalSeconds")] public float KeyframeIntervalSeconds { get; set; } = 1f;
}
