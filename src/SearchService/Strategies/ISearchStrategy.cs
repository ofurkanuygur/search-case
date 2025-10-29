using SearchCase.Search.Contracts.Models;

namespace SearchService.Strategies;

/// <summary>
/// Strategy pattern for polymorphic search implementations
/// </summary>
public interface ISearchStrategy
{
    /// <summary>
    /// Strategy name for logging/diagnostics
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Priority for strategy selection (higher = preferred)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this strategy can handle the request
    /// </summary>
    bool CanHandle(SearchRequest request);

    /// <summary>
    /// Execute search with this strategy
    /// </summary>
    Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
