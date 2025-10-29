using Microsoft.AspNetCore.Mvc;
using EventBusService.Events;
using EventBusService.Services;
using EventBusContracts;

namespace EventBusService.Controllers;

/// <summary>
/// Controller for publishing events to the message bus
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventPublisher eventPublisher,
        ILogger<EventsController> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publish a content changed event
    /// </summary>
    /// <param name="eventData">The event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the publish operation</returns>
    [HttpPost("content-changed")]
    public async Task<IActionResult> PublishContentChanged(
        [FromBody] ContentChangedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
        {
            return BadRequest("Event data is required");
        }

        try
        {
            _logger.LogInformation(
                "Received ContentChangedEvent for content {ContentId} - {ChangeType}",
                eventData.ContentId,
                eventData.ChangeType);

            await _eventPublisher.PublishAsync(eventData, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Event published successfully",
                eventId = eventData.EventId,
                contentId = eventData.ContentId,
                changeType = eventData.ChangeType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish ContentChangedEvent for content {ContentId}",
                eventData.ContentId);

            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish event",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Publish multiple content changed events in batch
    /// </summary>
    /// <param name="events">The events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the publish operation</returns>
    [HttpPost("content-changed/batch")]
    public async Task<IActionResult> PublishContentChangedBatch(
        [FromBody] List<ContentChangedEvent> events,
        CancellationToken cancellationToken = default)
    {
        if (events == null || !events.Any())
        {
            return BadRequest("At least one event is required");
        }

        try
        {
            _logger.LogInformation(
                "Received batch of {Count} ContentChangedEvents",
                events.Count);

            await _eventPublisher.PublishBatchAsync(events, cancellationToken);

            return Ok(new
            {
                success = true,
                message = $"Successfully published {events.Count} events",
                count = events.Count,
                eventIds = events.Select(e => e.EventId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish batch of {Count} ContentChangedEvents",
                events.Count);

            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish events",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Publish a batch content update event (optimized version with only IDs)
    /// </summary>
    /// <param name="eventData">The batch update event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the publish operation</returns>
    [HttpPost("batch")]
    public async Task<IActionResult> PublishBatchUpdate(
        [FromBody] ContentBatchUpdatedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
        {
            return BadRequest("Event data is required");
        }

        if (eventData.ContentIds == null || !eventData.ContentIds.Any())
        {
            return BadRequest("At least one content ID is required");
        }

        try
        {
            _logger.LogInformation(
                "Received ContentBatchUpdatedEvent with {Count} items - {ChangeType} from {Provider}",
                eventData.ContentIds.Count,
                eventData.ChangeType,
                eventData.SourceProvider ?? "unknown");

            await _eventPublisher.PublishAsync(eventData, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Batch event published successfully",
                batchId = eventData.BatchId,
                contentCount = eventData.ContentIds.Count,
                changeType = eventData.ChangeType,
                sourceProvider = eventData.SourceProvider
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish ContentBatchUpdatedEvent with {Count} items",
                eventData.ContentIds.Count);

            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish batch event",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "EventBusService",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}