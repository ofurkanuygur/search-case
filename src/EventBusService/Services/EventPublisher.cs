using MassTransit;

namespace EventBusService.Services;

/// <summary>
/// Implementation of event publisher using MassTransit and Kafka
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        IServiceProvider serviceProvider,
        ILogger<EventPublisher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default)
        where T : class
    {
        if (eventMessage == null)
        {
            _logger.LogWarning("Attempted to publish null event message");
            return;
        }

        try
        {
            _logger.LogInformation(
                "Publishing event: {EventType} - {EventMessage}",
                typeof(T).Name,
                eventMessage);

            var producer = _serviceProvider.GetRequiredService<ITopicProducer<T>>();
            await producer.Produce(eventMessage, cancellationToken);

            _logger.LogInformation(
                "Successfully published event: {EventType}",
                typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event: {EventType}",
                typeof(T).Name);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> eventMessages, CancellationToken cancellationToken = default)
        where T : class
    {
        var events = eventMessages?.ToList();

        if (events == null || !events.Any())
        {
            _logger.LogWarning("Attempted to publish empty batch of events");
            return;
        }

        _logger.LogInformation(
            "Publishing batch of {Count} {EventType} events",
            events.Count,
            typeof(T).Name);

        try
        {
            var producer = _serviceProvider.GetRequiredService<ITopicProducer<T>>();
            
            // Kafka producer is efficient with batching, we can just produce them
            var tasks = events.Select(e => producer.Produce(e, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Successfully published batch of {Count} {EventType} events",
                events.Count,
                typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish batch of {EventType} events",
                typeof(T).Name);
            throw;
        }
    }
}