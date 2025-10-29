namespace CacheWorker.Models;

/// <summary>
/// Content entity from database - includes pre-calculated score
/// This is the "Single Source of Truth" model
/// </summary>
public class ContentEntity
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty; // "video" or "article"
    public string SourceProvider { get; set; } = string.Empty;
    public double Score { get; set; } // Pre-calculated score from WriteService
    public string ContentHash { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public long Version { get; set; } = 1;
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Video specific fields
    public long? Views { get; set; }
    public int? Likes { get; set; }
    public string? Duration { get; set; } // ISO 8601 duration format

    // Article specific fields
    public int? ReadingTimeMinutes { get; set; }
    public int? Reactions { get; set; }
    public int? Comments { get; set; }
}