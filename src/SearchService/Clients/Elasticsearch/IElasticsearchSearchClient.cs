using SearchCase.Search.Contracts.Models;

namespace SearchService.Clients.Elasticsearch;

/// <summary>
/// Elasticsearch search client for full-text queries
/// </summary>
public interface IElasticsearchSearchClient
{
    /// <summary>
    /// Search with keyword and filters
    /// </summary>
    Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
