namespace EventBusService.Events;

/// <summary>
/// Event published when content is created or updated
/// As per theoretical diagram: "ContentChangedEvent"
/// </summary>
public class ContentChangedEvent
{
    /// <summary>
    /// Unique identifier of the content
    /// </summary>
    public string ContentId { get; set; } = string.Empty;

    /// <summary>
    /// Type of content (Video/Article)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Title of the content
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Type of change (Created/Updated/Deleted)
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Current score after recalculation
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// Previous score (for comparison)
    /// </summary>
    public decimal? PreviousScore { get; set; }

    /// <summary>
    /// When the content was published
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// Categories of the content
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Source provider (provider1, provider2, etc.)
    /// </summary>
    public string SourceProvider { get; set; } = string.Empty;

    /// <summary>
    /// Content hash for integrity
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTimeOffset EventTimestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Unique event ID for idempotency
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Version for event schema evolution
    /// </summary>
    public int EventVersion { get; set; } = 1;

    /// <summary>
    /// Optional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Create a new content created event
    /// </summary>
    public static ContentChangedEvent Created(
        string contentId,
        string contentType,
        string title,
        decimal score,
        DateTimeOffset publishedAt,
        List<string> categories,
        string sourceProvider,
        string contentHash,
        string? metadata = null)
    {
        return new ContentChangedEvent
        {
            ContentId = contentId,
            ContentType = contentType,
            Title = title,
            ChangeType = "Created",
            Score = score,
            PreviousScore = null,
            PublishedAt = publishedAt,
            Categories = categories,
            SourceProvider = sourceProvider,
            ContentHash = contentHash,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Create a content updated event
    /// </summary>
    public static ContentChangedEvent Updated(
        string contentId,
        string contentType,
        string title,
        decimal score,
        decimal previousScore,
        DateTimeOffset publishedAt,
        List<string> categories,
        string sourceProvider,
        string contentHash,
        string? metadata = null)
    {
        return new ContentChangedEvent
        {
            ContentId = contentId,
            ContentType = contentType,
            Title = title,
            ChangeType = "Updated",
            Score = score,
            PreviousScore = previousScore,
            PublishedAt = publishedAt,
            Categories = categories,
            SourceProvider = sourceProvider,
            ContentHash = contentHash,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Create a content deleted event
    /// </summary>
    public static ContentChangedEvent Deleted(
        string contentId,
        string contentType,
        string title,
        string sourceProvider)
    {
        return new ContentChangedEvent
        {
            ContentId = contentId,
            ContentType = contentType,
            Title = title,
            ChangeType = "Deleted",
            SourceProvider = sourceProvider
        };
    }

    public override string ToString()
    {
        return $"ContentChangedEvent: {ChangeType} - {ContentId} ({ContentType}) - Score: {Score}";
    }
}