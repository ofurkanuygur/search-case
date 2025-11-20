using MassTransit;
using EventBusContracts;

namespace EventBusService.Services;

/// <summary>
/// Implementation of event publisher using MassTransit and Apache Kafka
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ITopicProducer<ContentBatchUpdatedEvent> _kafkaProducer;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        ITopicProducer<ContentBatchUpdatedEvent> kafkaProducer,
        ILogger<EventPublisher> logger)
    {
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
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
                "Publishing event to Kafka: {EventType} - {EventMessage}",
                typeof(T).Name,
                eventMessage);

            // For now, only support ContentBatchUpdatedEvent
            if (eventMessage is ContentBatchUpdatedEvent batchEvent)
            {
                await _kafkaProducer.Produce(batchEvent, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Event type {EventType} is not supported for Kafka publishing", typeof(T).Name);
                return;
            }

            _logger.LogInformation(
                "Successfully published event to Kafka: {EventType}",
                typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event to Kafka: {EventType}",
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

        var publishTasks = events.Select(evt => PublishAsync(evt, cancellationToken));

        try
        {
            await Task.WhenAll(publishTasks);

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