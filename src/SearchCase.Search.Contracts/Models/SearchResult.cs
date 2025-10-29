namespace SearchCase.Search.Contracts.Models;

/// <summary>
/// Unified search result across all strategies
/// </summary>
public sealed record SearchResult
{
    /// <summary>
    /// Search result items
    /// </summary>
    public IReadOnlyList<ContentDto> Items { get; init; } = Array.Empty<ContentDto>();

    /// <summary>
    /// Pagination metadata
    /// </summary>
    public PaginationMetadata Pagination { get; init; } = default!;

    /// <summary>
    /// Search metadata and diagnostics
    /// </summary>
    public SearchMetadata Metadata { get; init; } = default!;
}
