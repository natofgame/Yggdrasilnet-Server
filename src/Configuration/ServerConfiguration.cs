using System.Text.Json.Serialization;

namespace Yggdrasilnet.Server.Configuration;

public sealed class ServerConfiguration {
    [JsonPropertyName("port")] public int Port { get; set; } = 9050;

    [JsonPropertyName("tickRate")] public int TickRate { get; set; } = 30;
}