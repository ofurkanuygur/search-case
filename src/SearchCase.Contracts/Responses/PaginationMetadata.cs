namespace SearchCase.Contracts.Responses;

/// <summary>
/// Pagination metadata for content listing
/// </summary>
public sealed class PaginationMetadata
{
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalItems / PageSize)
        : 0;

    public bool HasNext => CurrentPage < TotalPages;
    public bool HasPrevious => CurrentPage > 1;
}
