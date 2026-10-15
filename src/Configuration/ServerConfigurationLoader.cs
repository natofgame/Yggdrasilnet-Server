using System.Text.Json;

namespace Yggdrasilnet.Server.Configuration;

public static class ServerConfigurationLoader {
    public static ServerConfiguration Load(string path) {
        if (!File.Exists(path)) {
            return new ServerConfiguration();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ServerConfiguration>(json) ?? new ServerConfiguration();
    }
}