namespace Yggdrasilnet.Server.Bootstrap;

/// <summary>
///     Represents a service that participates in the application lifetime.
/// </summary>
public interface IHostedService {
    /// <summary>
    ///     Starts the service.
    /// </summary>
    void Start();

    /// <summary>
    ///     Stops the service.
    /// </summary>
    void Stop();
}