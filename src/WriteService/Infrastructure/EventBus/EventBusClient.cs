using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using EventBusContracts;

namespace WriteService.Infrastructure.EventBus;

/// <summary>
/// EventBus client implementation with Circuit Breaker pattern
/// As per theoretical diagram: "Publish Event" to EventBus
/// </summary>
public sealed class EventBusClient : IEventBusClient
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;
    private readonly ILogger<EventBusClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private CircuitBreakerPolicy<HttpResponseMessage>? _circuitBreaker;

    public EventBusClient(
        HttpClient httpClient,
        ILogger<EventBusClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure Circuit Breaker
        _circuitBreakerPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3, // Open after 3 failures
                durationOfBreak: TimeSpan.FromSeconds(30), // Stay open for 30 seconds
                onBreak: (result, duration) =>
                {
                    _logger.LogWarning(
                        "Circuit breaker opened for {Duration}s due to failures",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset, connection restored");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker is half-open, testing connection");
                });

        // Store circuit breaker for state inspection
        if (_circuitBreakerPolicy is CircuitBreakerPolicy<HttpResponseMessage> cb)
        {
            _circuitBreaker = cb;
        }
    }

    public async Task PublishContentChangedAsync(
        List<string> createdIds,
        List<string> updatedIds,
        string? sourceProvider = null,
        CancellationToken cancellationToken = default)
    {
        if (!createdIds.Any() && !updatedIds.Any())
        {
            _logger.LogDebug("No content changes to publish");
            return;
        }

        try
        {
            // Create simple batch event with only IDs and change type
            var batchEvent = new ContentBatchUpdatedEvent
            {
                ContentIds = createdIds.Concat(updatedIds).ToList(),
                ChangeType = GetChangeType(createdIds, updatedIds),
                SourceProvider = sourceProvider,
                ProcessedAt = DateTimeOffset.UtcNow,
                BatchId = Guid.NewGuid()
            };

            // Add metadata for mixed changes
            if (createdIds.Any() && updatedIds.Any())
            {
                batchEvent.Metadata = new Dictionary<string, object>
                {
                    { "createdCount", createdIds.Count },
                    { "updatedCount", updatedIds.Count }
                };
            }

            _logger.LogInformation(
                "Publishing batch event: {CreatedCount} created, {UpdatedCount} updated, ContentIds: {ContentIds}",
                createdIds.Count,
                updatedIds.Count,
                string.Join(", ", batchEvent.ContentIds.Take(5)));

            // Execute with circuit breaker - using the correct endpoint
            var response = await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                var json = JsonSerializer.Serialize(batchEvent, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                return await _httpClient.PostAsync(
                    "/api/events/batch",  // Using the correct EventBusService endpoint
                    content,
                    cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully published batch event {BatchId} with {Count} items",
                    batchEvent.BatchId,
                    batchEvent.ContentIds.Count);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to publish event. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode,
                    errorContent);
            }
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex,
                "Circuit breaker is open. EventBus is unavailable. Event not published");

            // Could implement fallback strategy here (e.g., save to local queue)
            await HandleCircuitBreakerOpenAsync(createdIds, updatedIds, sourceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish content changed event");
            throw;
        }
    }

    public CircuitState GetCircuitState()
    {
        return _circuitBreaker?.CircuitState ?? CircuitState.Closed;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string GetChangeType(List<string> createdIds, List<string> updatedIds)
    {
        if (createdIds.Any() && updatedIds.Any())
            return "Mixed";
        if (createdIds.Any())
            return "Created";
        if (updatedIds.Any())
            return "Updated";
        return "None";
    }

    private async Task HandleCircuitBreakerOpenAsync(
        List<string> createdIds,
        List<string> updatedIds,
        string? sourceProvider)
    {
        // Fallback strategy when circuit breaker is open
        // Option 1: Save to local queue/database for retry
        // Option 2: Log and alert
        // Option 3: Send to dead letter queue

        _logger.LogWarning(
            "Circuit breaker open. Fallback: Logging {CreatedCount} created, {UpdatedCount} updated items for manual recovery",
            createdIds.Count,
            updatedIds.Count);

        // TODO: Implement persistent fallback storage
        await Task.CompletedTask;
    }

}