namespace EventBusService.Services;

/// <summary>
/// Interface for publishing events to the message bus
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish an event to the message bus
    /// </summary>
    /// <typeparam name="T">Type of the event</typeparam>
    /// <param name="eventMessage">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Publish multiple events in batch
    /// </summary>
    /// <typeparam name="T">Type of the events</typeparam>
    /// <param name="eventMessages">The events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishBatchAsync<T>(IEnumerable<T> eventMessages, CancellationToken cancellationToken = default)
        where T : class;
}