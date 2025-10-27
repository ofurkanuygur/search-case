using System.Text.Json;
using JsonProviderMicroservice.Configuration;
using JsonProviderMicroservice.Models;
using Microsoft.Extensions.Options;

namespace JsonProviderMicroservice.HttpClients;

/// <summary>
/// HTTP client for fetching data from external provider API
/// Implements resilience patterns via Polly (configured in Extensions)
/// </summary>
public sealed class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly ILogger<ExternalApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExternalApiClient(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> settings,
        ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Fetches content data from the external provider
    /// </summary>
    public async Task<ContentResponse> GetContentAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = _settings.BaseUrl;

        try
        {
            if (_settings.EnableDetailedLogging)
            {
                _logger.LogInformation("Fetching content from external API: {Url}", requestUri);
            }

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (_settings.EnableDetailedLogging)
            {
                _logger.LogDebug("Response status code: {StatusCode}", response.StatusCode);
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (_settings.EnableDetailedLogging)
            {
                _logger.LogDebug("Response content length: {Length} bytes", content.Length);
            }

            var result = JsonSerializer.Deserialize<ContentResponse>(content, _jsonOptions);

            if (result == null)
            {
                _logger.LogWarning("Deserialization returned null for URL: {Url}", requestUri);
                return new ContentResponse();
            }

            _logger.LogInformation(
                "Successfully fetched {Count} content items from external API",
                result.Contents.Count);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP request failed for URL: {Url}. Status: {Status}",
                requestUri,
                ex.StatusCode);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to deserialize response from URL: {Url}",
                requestUri);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex,
                "Request to {Url} was canceled or timed out",
                requestUri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error fetching content from URL: {Url}",
                requestUri);
            throw;
        }
    }
}
