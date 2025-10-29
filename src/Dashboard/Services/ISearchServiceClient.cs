using SearchCase.Search.Contracts.Models;

namespace Dashboard.Services;

/// <summary>
/// Client for communicating with SearchService API
/// </summary>
public interface ISearchServiceClient
{
    Task<SearchResult?> SearchAsync(
        string? keyword = null,
        string? type = null,
        string? sort = null,
        int page = 1,
        int pageSize = 20,
        double? minScore = null,
        double? maxScore = null,
        CancellationToken cancellationToken = default);
}
