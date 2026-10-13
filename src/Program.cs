using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
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

        const int port = 9050;
        const int tickRate = 30;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        var host = new ServerHost(port, tickRate);
        host.Run(cts.Token);
    }
}
