using System.Collections.Concurrent;
using Serilog;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Network;

namespace Yggdrasilnet.Server.Bootstrap;

/// <summary>
///     Hosts the game server as a managed application service.
/// </summary>
public sealed class ServerHost(int port, int tickRate) : IHostedService, IDisposable {
    private readonly ConcurrentQueue<IServerEvent> _events = new();
    private Thread? _consoleThread;
    private bool _disposed;
    private Thread? _gameLoopThread;
    private GameLoop? _loop;
    private CancellationTokenSource? _runCts;

    private ServerTransport? _server;
    private Simulation? _simulation;
    private bool _stopped;

    /// <summary>
    ///     Stops the game loop and releases all resources.
    /// </summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        Stop();
        _runCts?.Dispose();
    }

    /// <inheritdoc />
    public void Start() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ProtocolRegistry.Registers();

        _server = new ServerTransport(_events);
        _simulation = new Simulation(_events, _server, tickRate);
        _loop = new GameLoop(_server, _simulation, tickRate);

        _server.Start(port);
        Log.Information("Game loop running at {TickRate} tps", tickRate);

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

    /// <inheritdoc />
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

    /// <summary>
    ///     Runs the game loop until the token is canceled.
    /// </summary>
    /// <param name="token"></param>
    private void RunLoop(CancellationToken token) {
        try {
            _loop!.Run(token);
        }
        finally {
            _server?.Stop();
            Log.Information("Game loop stopped.");
        }
    }

    /// <summary>
    ///     Runs the console until the token is canceled.
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