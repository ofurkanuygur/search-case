using Polly.CircuitBreaker;

namespace WriteService.Infrastructure.EventBus;

/// <summary>
/// Client interface for publishing events to EventBus service
/// Implements Circuit Breaker pattern for resilience
/// </summary>
public interface IEventBusClient
{
    /// <summary>
    /// Publish content changed events to EventBus
    /// </summary>
    /// <param name="createdIds">IDs of newly created contents</param>
    /// <param name="updatedIds">IDs of updated contents</param>
    /// <param name="sourceProvider">Source provider name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishContentChangedAsync(
        List<string> createdIds,
        List<string> updatedIds,
        string? sourceProvider = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get circuit breaker state
    /// </summary>
    CircuitState GetCircuitState();

    /// <summary>
    /// Get health status of EventBus connection
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}