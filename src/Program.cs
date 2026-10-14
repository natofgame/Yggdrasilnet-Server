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

        try {
            var configPath = Path.Combine(AppContext.BaseDirectory, "server.config.json");
            var configuration = ServerConfigurationLoader.Load(configPath);

            using var host = new ServerHost(configuration.Port, configuration.TickRate);
            using var app = new ServerApplication();

            app.Register(host);
            app.Run();
        }
        catch (Exception exception) {
            Log.Fatal(exception, "Application terminated unexpectedly");
            throw;
        }
        finally {
            Log.CloseAndFlush();
        }
    }
}
