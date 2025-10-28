using Microsoft.Extensions.Logging;
using SearchCase.Contracts.Models;
using SearchCase.Contracts.Responses;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace WriteService.Application.Services;

/// <summary>
/// Implementation of IProviderClient using HttpClient
/// Calls JSON and XML provider microservices
/// </summary>
public sealed class ProviderClient : IProviderClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProviderClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProviderClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ProviderClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure JSON options matching provider microservices
        // Remove custom converter to avoid duplicate type property issues
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new SearchCase.Contracts.Converters.Iso8601DurationConverter(),
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    public async Task<ProviderResponse?> FetchFromJsonProviderAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching data from JSON Provider");

            var client = _httpClientFactory.CreateClient("JsonProvider");
            var jsonString = await client.GetStringAsync("/api/provider/data", cancellationToken);

            // Parse JSON manually to handle type discrimination
            var response = ParseProviderResponse(jsonString);

            if (response != null)
            {
                _logger.LogInformation(
                    "Successfully fetched {Count} items from JSON Provider",
                    response.Items.Count);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from JSON Provider");
            return null;
        }
    }

    public async Task<ProviderResponse?> FetchFromXmlProviderAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching data from XML Provider");

            var client = _httpClientFactory.CreateClient("XmlProvider");
            var jsonString = await client.GetStringAsync("/api/provider/data", cancellationToken);

            // Parse JSON manually to handle type discrimination
            var response = ParseProviderResponse(jsonString);

            if (response != null)
            {
                _logger.LogInformation(
                    "Successfully fetched {Count} items from XML Provider",
                    response.Items.Count);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from XML Provider");
            return null;
        }
    }

    public async Task<ProviderResponse> FetchFromAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching data from all providers");

        // Fetch from both providers in parallel
        var tasks = new[]
        {
            FetchFromJsonProviderAsync(cancellationToken),
            FetchFromXmlProviderAsync(cancellationToken)
        };

        var results = await Task.WhenAll(tasks);

        // Merge results
        var allItems = results
            .Where(r => r != null)
            .SelectMany(r => r!.Items)
            .ToList();

        _logger.LogInformation("Total items fetched from all providers: {Count}", allItems.Count);

        return new ProviderResponse
        {
            Items = allItems,
            Pagination = null!,
            Provider = null!
        };
    }

    private ProviderResponse? ParseProviderResponse(string jsonString)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            var response = new ProviderResponse
            {
                Items = new List<CanonicalContent>(),
                Errors = new List<string>()
            };

            // Parse items array
            if (root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    if (!item.TryGetProperty("type", out var typeElement))
                        continue;

                    var typeStr = typeElement.GetString();
                    CanonicalContent? content = null;

                    if (typeStr == "video")
                    {
                        content = JsonSerializer.Deserialize<CanonicalVideoContent>(item.GetRawText(), _jsonOptions);
                    }
                    else if (typeStr == "article")
                    {
                        content = JsonSerializer.Deserialize<CanonicalArticleContent>(item.GetRawText(), _jsonOptions);
                    }

                    if (content != null)
                    {
                        response.Items.Add(content);
                    }
                }
            }

            // Parse pagination
            if (root.TryGetProperty("pagination", out var paginationElement))
            {
                response.Pagination = JsonSerializer.Deserialize<PaginationMetadata>(
                    paginationElement.GetRawText(), _jsonOptions) ?? new PaginationMetadata();
            }

            // Parse provider
            if (root.TryGetProperty("provider", out var providerElement))
            {
                response.Provider = JsonSerializer.Deserialize<ProviderMetadata>(
                    providerElement.GetRawText(), _jsonOptions) ?? new ProviderMetadata();
            }

            // Parse errors
            if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errorsElement.EnumerateArray())
                {
                    var errorStr = error.GetString();
                    if (!string.IsNullOrEmpty(errorStr))
                    {
                        response.Errors.Add(errorStr);
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse provider response JSON");
            return null;
        }
    }
}
