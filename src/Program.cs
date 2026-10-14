using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Yggdrasilnet.Server.Configuration;
using Yggdrasilnet.Server.Hosting;

namespace Yggdrasilnet.Server;

public static class Program {
    public static void Main(string[] args) {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        var configPath = Path.Combine(AppContext.BaseDirectory, "server.config.json");
        var configuration = ServerConfigurationLoader.Load(configPath);

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var host = new ServerHost(configuration.Port, configuration.TickRate);
        host.Run(cancellation.Token);
    }
}
