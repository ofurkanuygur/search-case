namespace SearchWorker.Models;

/// <summary>
/// Search query result with pagination
/// </summary>
public sealed class SearchResult
{
    public List<SearchDocument> Documents { get; init; } = new();
    public long TotalHits { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalHits / PageSize);
    public TimeSpan QueryTime { get; init; }
}
