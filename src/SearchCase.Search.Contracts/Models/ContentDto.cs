using SearchCase.Search.Contracts.Enums;

namespace SearchCase.Search.Contracts.Models;

/// <summary>
/// Content data transfer object
/// </summary>
public sealed record ContentDto
{
    /// <summary>
    /// Unique content identifier
    /// </summary>
    public string Id { get; init; } = default!;

    /// <summary>
    /// Content title
    /// </summary>
    public string Title { get; init; } = default!;

    /// <summary>
    /// Content type (video/article)
    /// </summary>
    public ContentType Type { get; init; }

    /// <summary>
    /// Calculated score (case formula)
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Publication date
    /// </summary>
    public DateTimeOffset PublishedAt { get; init; }

    /// <summary>
    /// Categories/tags
    /// </summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Source provider name
    /// </summary>
    public string SourceProvider { get; init; } = default!;

    /// <summary>
    /// Type-specific metrics (views, likes, reactions, etc.)
    /// </summary>
    public IReadOnlyDictionary<string, object> Metrics { get; init; } =
        new Dictionary<string, object>();
}
