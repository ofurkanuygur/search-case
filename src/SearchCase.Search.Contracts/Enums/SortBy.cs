namespace SearchCase.Search.Contracts.Enums;

/// <summary>
/// Sort options for search results
/// </summary>
public enum SortBy
{
    /// <summary>
    /// Sort by pre-calculated score (default)
    /// </summary>
    Score,

    /// <summary>
    /// Sort by Elasticsearch relevance (BM25)
    /// </summary>
    Relevance,

    /// <summary>
    /// Sort by published date
    /// </summary>
    Date
}
