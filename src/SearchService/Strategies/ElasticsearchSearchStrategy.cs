using SearchCase.Search.Contracts.Models;
using SearchService.Clients.Elasticsearch;

namespace SearchService.Strategies;

/// <summary>
/// Elasticsearch search strategy for keyword-based queries
/// Supports full-text search, fuzzy matching, and relevance scoring
/// </summary>
public sealed class ElasticsearchSearchStrategy : ISearchStrategy
{
    private readonly IElasticsearchSearchClient _elasticsearchClient;
    private readonly ILogger<ElasticsearchSearchStrategy> _logger;

    public string Name => "Elasticsearch";
    public int Priority => 1; // Lowest priority (comprehensive but slower)

    public ElasticsearchSearchStrategy(
        IElasticsearchSearchClient elasticsearchClient,
        ILogger<ElasticsearchSearchStrategy> logger)
    {
        _elasticsearchClient = elasticsearchClient ?? throw new ArgumentNullException(nameof(elasticsearchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(SearchRequest request)
    {
        // Always can handle keyword searches
        // Also fallback for any query Redis can't handle
        return true; // Universal fallback
    }

    public async Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing Elasticsearch search: keyword={Keyword}, type={Type}, sort={Sort}, page={Page}",
            request.Keyword, request.Type, request.Sort, request.Page);

        try
        {
            var result = await _elasticsearchClient.SearchAsync(request, cancellationToken);

            // Update metadata to reflect strategy
            result = result with
            {
                Metadata = result.Metadata with
                {
                    Strategy = Name
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch search failed");
            throw;
        }
    }
}
