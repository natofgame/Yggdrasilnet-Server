using System.Collections.Concurrent;
using Serilog;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Network;
using Yggdrasilnet.Server.Runtime;

namespace Yggdrasilnet.Server.Hosting;

public sealed class ServerHost(int port, int tickRate) {
    private readonly ConcurrentQueue<IServerEvent> _events = new();

    /// <summary>
    ///  Starts the server.
    /// </summary>
    /// <param name="token"></param>
    public void Run(CancellationToken token) {
        ProtocolRegistry.Registers();

        var server = new ServerTransport(_events);
        var simulation = new SimulationEngine(_events, server, tickRate);
        var loop = new GameLoop(server, simulation, tickRate);

        server.Start(port);
        Log.Information("Game loop running at {TickRate} tps", tickRate);

        var consoleThread = new Thread(() => RunConsole(token)) { IsBackground = true };
        consoleThread.Start();

        try {
            loop.Run(token);
        }
        finally {
            server.Stop();
            Log.Information("Server stopped.");
        }
    }

    /// <summary>
    ///  Runs the console.
    /// </summary>
    /// <param name="token"></param>
    private void RunConsole(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            var line = Console.ReadLine();
            if (line is null) {
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            _events.Enqueue(new ConsoleCommandEvent(line));
        }
    }
}
