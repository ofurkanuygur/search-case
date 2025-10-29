using SearchCase.Search.Contracts.Models;

namespace SearchService.Orchestration;

/// <summary>
/// Orchestrates strategy selection and execution
/// </summary>
public interface ISearchOrchestrator
{
    /// <summary>
    /// Execute search with automatic strategy selection
    /// </summary>
    Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
