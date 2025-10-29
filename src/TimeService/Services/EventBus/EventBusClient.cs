using System.Net.Http.Json;
using System.Text.Json;
using EventBusContracts;

namespace TimeService.Services.EventBus;

/// <summary>
/// HTTP client for publishing events to EventBusService
/// Uses Polly for resilience (retry + circuit breaker)
/// SOLID: Single Responsibility - Only handles event publishing via HTTP
/// </summary>
public sealed class EventBusClient : IEventBusClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventBusClient> _logger;

    public EventBusClient(
        HttpClient httpClient,
        ILogger<EventBusClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> PublishScoreUpdatesAsync(
        ContentBatchUpdatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Publishing ContentBatchUpdatedEvent (ScoreUpdated) for {Count} content items to EventBusService",
                @event.ContentIds.Count);

            // POST to EventBusService - same endpoint as WriteService uses
            var response = await _httpClient.PostAsJsonAsync(
                "/api/events/batch",
                @event,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully published ContentBatchUpdatedEvent to EventBusService " +
                    "(Status: {StatusCode}, BatchId: {BatchId}, Count: {Count})",
                    response.StatusCode,
                    @event.BatchId,
                    @event.ContentIds.Count);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to publish ContentBatchUpdatedEvent to EventBusService. " +
                "Status: {StatusCode}, Response: {Response}",
                response.StatusCode,
                errorContent);

            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP request failed when publishing ContentBatchUpdatedEvent to EventBusService");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                ex,
                "Request timeout when publishing ContentBatchUpdatedEvent to EventBusService");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error when publishing ContentBatchUpdatedEvent to EventBusService");
            return false;
        }
    }
}
