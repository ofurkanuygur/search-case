namespace SearchCase.HangfireWorker.Services;

/// <summary>
/// Interface for microservice communication
/// </summary>
public interface IMicroserviceClient
{
    /// <summary>
    /// Trigger a microservice with payload
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="payload">Request payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response content</returns>
    Task<string> TriggerAsync(
        string serviceName,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a microservice is healthy
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if healthy, false otherwise</returns>
    Task<bool> CheckHealthAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
