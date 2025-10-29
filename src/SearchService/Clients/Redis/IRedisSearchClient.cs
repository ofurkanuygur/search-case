using SearchCase.Search.Contracts.Enums;
using SearchCase.Search.Contracts.Models;

namespace SearchService.Clients.Redis;

/// <summary>
/// Redis search client for fast queries
/// </summary>
public interface IRedisSearchClient
{
    /// <summary>
    /// Get top content by score
    /// </summary>
    Task<IReadOnlyList<ContentDto>> GetTopByScoreAsync(
        ContentType? type,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get content by ID
    /// </summary>
    Task<ContentDto?> GetByIdAsync(
        string contentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total count for type filter
    /// </summary>
    Task<int> GetTotalCountAsync(
        ContentType? type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cached query result
    /// </summary>
    Task<SearchResult?> GetCachedQueryAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set cached query result
    /// </summary>
    Task SetCachedQueryAsync(
        SearchRequest request,
        SearchResult result,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
