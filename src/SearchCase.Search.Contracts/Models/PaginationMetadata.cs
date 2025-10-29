namespace SearchCase.Search.Contracts.Models;

/// <summary>
/// Pagination metadata for search results
/// </summary>
public sealed record PaginationMetadata
{
    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    /// <summary>
    /// Has previous page
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// Has next page
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>
    /// First item index (1-based)
    /// </summary>
    public int FirstItemIndex => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;

    /// <summary>
    /// Last item index (1-based)
    /// </summary>
    public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalItems);
}
