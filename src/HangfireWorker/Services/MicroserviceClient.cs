using System.Net.Http.Json;
using System.Text.Json;

namespace SearchCase.HangfireWorker.Services;

/// <summary>
/// HTTP client for microservice communication
/// Implements resilient communication patterns
/// </summary>
public sealed class MicroserviceClient : IMicroserviceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MicroserviceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public MicroserviceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<MicroserviceClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> TriggerAsync(
        string serviceName,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));

        var client = _httpClientFactory.CreateClient(serviceName);

        _logger.LogInformation(
            "Triggering {ServiceName} at endpoint {Endpoint} with payload: {Payload}",
            serviceName,
            endpoint,
            payload != null ? JsonSerializer.Serialize(payload, JsonOptions) : "null");

        try
        {
            HttpResponseMessage response;

            if (payload != null)
            {
                response = await client.PostAsJsonAsync(endpoint, payload, JsonOptions, cancellationToken);
            }
            else
            {
                response = await client.PostAsync(endpoint, null, cancellationToken);
            }

            // Ensure success status code or throw
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully triggered {ServiceName}. Status: {StatusCode}, Response length: {Length} characters",
                serviceName,
                response.StatusCode,
                content.Length);

            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error while triggering {ServiceName} at {Endpoint}. Status: {StatusCode}",
                serviceName,
                endpoint,
                ex.StatusCode);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                ex,
                "Timeout while triggering {ServiceName} at {Endpoint}",
                serviceName,
                endpoint);
            throw new TimeoutException($"Request to {serviceName} timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while triggering {ServiceName} at {Endpoint}",
                serviceName,
                endpoint);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckHealthAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        try
        {
            var client = _httpClientFactory.CreateClient(serviceName);
            var response = await client.GetAsync("/health", cancellationToken);

            var isHealthy = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "{ServiceName} health check result: {IsHealthy} (Status: {StatusCode})",
                serviceName,
                isHealthy ? "Healthy" : "Unhealthy",
                response.StatusCode);

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{ServiceName} health check failed",
                serviceName);
            return false;
        }
    }
}
