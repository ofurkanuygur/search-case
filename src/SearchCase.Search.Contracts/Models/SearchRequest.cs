using SearchCase.Search.Contracts.Enums;

namespace SearchCase.Search.Contracts.Models;

/// <summary>
/// Search request with validation
/// </summary>
public sealed record SearchRequest
{
    /// <summary>
    /// Search keyword (optional for simple queries)
    /// </summary>
    public string? Keyword { get; init; }

    /// <summary>
    /// Filter by content type (optional)
    /// </summary>
    public ContentType? Type { get; init; }

    /// <summary>
    /// Sort option (default: Score)
    /// </summary>
    public SortBy Sort { get; init; } = SortBy.Score;

    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (default: 20, max: 100)
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Calculate query complexity for routing decisions
    /// </summary>
    public int ComplexityScore
    {
        get
        {
            var score = 0;
            if (!string.IsNullOrWhiteSpace(Keyword)) score += 3;
            if (Type.HasValue) score += 1;
            if (Sort != SortBy.Score) score += 1;
            return score;
        }
    }
}
