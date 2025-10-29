using EventBusContracts;

namespace TimeService.Services.EventBus;

/// <summary>
/// Event bus client interface for publishing score update events
/// SOLID: Interface Segregation - Only score update event publishing
/// </summary>
public interface IEventBusClient
{
    /// <summary>
    /// Publishes ContentBatchUpdatedEvent to EventBusService
    /// Triggers downstream processing in CacheWorker and SearchWorker
    /// ChangeType will be set to "ScoreUpdated" to differentiate from content changes
    /// </summary>
    /// <param name="event">The batch update event with score changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishScoreUpdatesAsync(
        ContentBatchUpdatedEvent @event,
        CancellationToken cancellationToken = default);
}
