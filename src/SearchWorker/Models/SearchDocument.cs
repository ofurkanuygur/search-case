namespace SearchWorker.Models;

/// <summary>
/// Elasticsearch document model
/// Represents content in search index
/// </summary>
public sealed class SearchDocument
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string SourceProvider { get; init; } = string.Empty;
    public double Score { get; init; }
    public List<string> Categories { get; init; } = new();
    public DateTimeOffset PublishedAt { get; init; }
    public DateTimeOffset IndexedAt { get; init; }

    // Searchable fields
    public string SearchableText { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();

    // Metrics
    public long? Views { get; init; }
    public int? Likes { get; init; }
    public int? Comments { get; init; }
    public int? Reactions { get; init; }
    public int? ReadingTimeMinutes { get; init; }
    public string? Duration { get; init; }
}