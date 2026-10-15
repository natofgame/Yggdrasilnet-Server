using Serilog;

namespace Yggdrasilnet.Server.Bootstrap;

/// <summary>
///     Orchestrates the server application lifetime: signal handling, service startup/shutdown,
///     and graceful termination.
/// </summary>
public sealed class ServerApplication : IDisposable {
    private readonly List<IHostedService> _services = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private bool _disposed;
    private bool _servicesStopped;

    /// <summary>
    ///     Token that is canceled when the application is requested to shut down.
    /// </summary>
    public CancellationToken ShutdownToken => _shutdownCts.Token;

    /// <summary>
    ///     Disposes of the application.
    /// </summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        RequestShutdown();
        StopServices();
        _shutdownCts.Dispose();
    }

    /// <summary>
    ///     Registers a hosted service.
    /// </summary>
    public ServerApplication Register(IHostedService service) {
        ArgumentNullException.ThrowIfNull(service);
        _services.Add(service);
        return this;
    }

    /// <summary>
    ///     Runs the application until shutdown is requested.
    /// </summary>
    public void Run() {
        RegisterShutdownHandlers();

        try {
            StartServices();
            WaitForShutdown();
        }
        finally {
            StopServices();
            UnregisterShutdownHandlers();
        }
    }

    /// <summary>
    ///     Requests the application to shut down.
    /// </summary>
    private void RequestShutdown() {
        if (_disposed) {
            return;
        }

        try {
            _shutdownCts.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed; nothing to do.
        }
    }

    /// <summary>
    ///     Registers handlers for shutdown signals.
    /// </summary>
    private void RegisterShutdownHandlers() {
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    /// <summary>
    ///     Unregisters handlers for shutdown signals.
    /// </summary>
    private void UnregisterShutdownHandlers() {
        Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
    }

    /// <summary>
    ///     Handles Ctrl+C and other signals to request shutdown.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs) {
        eventArgs.Cancel = true;
        RequestShutdown();
    }

    /// <summary>
    ///     Handles process exit to request shutdown.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    private void OnProcessExit(object? sender, EventArgs eventArgs) {
        RequestShutdown();
    }

    /// <summary>
    ///     Starts all registered services.
    /// </summary>
    private void StartServices() {
        Log.Information("Starting server application...");

        foreach (var service in _services)
            service.Start();

        Log.Information("Server application started. Press Ctrl+C to stop.");
    }

    /// <summary>
    ///     Blocks until shutdown is requested.
    /// </summary>
    private void WaitForShutdown() {
        _shutdownCts.Token.WaitHandle.WaitOne();
    }

    /// <summary>
    ///     Stops all registered services.
    /// </summary>
    private void StopServices() {
        if (_servicesStopped) {
            return;
        }

        _servicesStopped = true;
        Log.Information("Stopping server application...");

        foreach (var service in _services.AsEnumerable().Reverse())
            try {
                service.Stop();
            }
            catch (Exception exception) {
                Log.Error(exception, "Error stopping service {ServiceType}", service.GetType().Name);
            }

        Log.Information("Server application stopped.");
    }
}