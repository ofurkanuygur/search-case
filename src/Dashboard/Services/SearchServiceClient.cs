using SearchCase.Search.Contracts.Models;
using System.Net.Http.Json;
using System.Web;

namespace Dashboard.Services;

/// <summary>
/// HTTP client for SearchService API
/// </summary>
public sealed class SearchServiceClient : ISearchServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchServiceClient> _logger;

    public SearchServiceClient(HttpClient httpClient, ILogger<SearchServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SearchResult?> SearchAsync(
        string? keyword = null,
        string? type = null,
        string? sort = null,
        int page = 1,
        int pageSize = 20,
        double? minScore = null,
        double? maxScore = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build query string
            var queryParams = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrWhiteSpace(keyword))
                queryParams["keyword"] = keyword;

            if (!string.IsNullOrWhiteSpace(type))
                queryParams["type"] = type;

            if (!string.IsNullOrWhiteSpace(sort))
                queryParams["sort"] = sort;

            queryParams["page"] = page.ToString();
            queryParams["pageSize"] = pageSize.ToString();

            if (minScore.HasValue)
                queryParams["minScore"] = minScore.Value.ToString();

            if (maxScore.HasValue)
                queryParams["maxScore"] = maxScore.Value.ToString();

            var queryString = queryParams.ToString();
            var requestUri = $"/api/search?{queryString}";

            _logger.LogInformation("Calling SearchService: {RequestUri}", requestUri);

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "SearchService returned error: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResult>(cancellationToken);

            _logger.LogInformation(
                "SearchService returned {TotalItems} items in {Latency}ms",
                result?.Pagination?.TotalItems ?? 0,
                result?.Metadata?.LatencyMs ?? 0);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling SearchService");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SearchService");
            return null;
        }
    }
}
