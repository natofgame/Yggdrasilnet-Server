using System.Collections.Concurrent;
using Serilog;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Network;
using Yggdrasilnet.Server.Runtime;

namespace Yggdrasilnet.Server.Hosting;

/// <summary>
///     Hosts the game server as a managed application service.
/// </summary>
public sealed class ServerHost : IHostedService, IDisposable {
    private readonly ConcurrentQueue<IServerEvent> _events = new();
    private readonly int _port;
    private readonly int _tickRate;

    private ServerTransport? _server;
    private SimulationEngine? _simulation;
    private GameLoop? _loop;
    private Thread? _gameLoopThread;
    private Thread? _consoleThread;
    private CancellationTokenSource? _runCts;
    private bool _disposed;
    private bool _stopped;

    public ServerHost(int port, int tickRate) {
        _port = port;
        _tickRate = tickRate;
    }

    /// <summary>
    ///     Starts the transport, simulation and background threads.
    /// </summary>
    public void Start() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ProtocolRegistry.Registers();

        _server = new ServerTransport(_events);
        _simulation = new SimulationEngine(_events, _server, _tickRate);
        _loop = new GameLoop(_server, _simulation, _tickRate);

        _server.Start(_port);
        Log.Information("Game loop running at {TickRate} tps", _tickRate);

        _runCts = new CancellationTokenSource();

        _consoleThread = new Thread(() => RunConsole(_runCts.Token)) {
            IsBackground = true,
            Name = "ServerHost.Console"
        };
        _consoleThread.Start();

        _gameLoopThread = new Thread(() => RunLoop(_runCts.Token)) {
            IsBackground = true,
            Name = "ServerHost.GameLoop"
        };
        _gameLoopThread.Start();
    }

    /// <summary>
    ///     Requests the game loop to stop and waits for background threads to exit.
    /// </summary>
    public void Stop() {
        if (_stopped) {
            return;
        }

        _stopped = true;
        _runCts?.Cancel();

        if (_consoleThread is not null && _consoleThread.IsAlive) {
            _consoleThread.Join(TimeSpan.FromSeconds(2));
        }

        if (_gameLoopThread is not null && _gameLoopThread.IsAlive) {
            _gameLoopThread.Join(TimeSpan.FromSeconds(10));
        }

        _server?.Stop();
        Log.Information("Server stopped.");
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        Stop();
        _runCts?.Dispose();
    }

    private void RunLoop(CancellationToken token) {
        try {
            _loop!.Run(token);
        }
        finally {
            _server?.Stop();
            Log.Information("Game loop stopped.");
        }
    }

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
