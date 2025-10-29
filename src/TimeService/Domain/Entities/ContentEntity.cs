using System.Text.Json;
using TimeService.Domain.Enums;
using TimeService.Domain.ValueObjects;

namespace TimeService.Domain.Entities;

/// <summary>
/// Aggregate root representing a content item (Video or Article)
/// Maps to PostgreSQL 'contents' table
/// Encapsulates business rules and invariants
/// </summary>
public sealed class ContentEntity
{
    /// <summary>
    /// Unique identifier from provider (e.g., "video-001")
    /// </summary>
    public string Id { get; private set; } = null!;

    /// <summary>
    /// Type of content (Video or Article)
    /// </summary>
    public ContentType Type { get; private set; }

    /// <summary>
    /// Content title
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// When the content was originally published
    /// </summary>
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>
    /// Categories/tags associated with the content
    /// </summary>
    public List<string> Categories { get; private set; } = new();

    /// <summary>
    /// Source provider identifier ('json-provider' or 'xml-provider')
    /// </summary>
    public string SourceProvider { get; private set; } = null!;

    /// <summary>
    /// Metrics stored as JSONB in PostgreSQL
    /// - Video: { views, likes, duration }
    /// - Article: { readingTimeMinutes, reactions, comments }
    /// </summary>
    public JsonElement Metrics { get; private set; }

    /// <summary>
    /// Calculated score value object
    /// </summary>
    public Score Score { get; private set; } = Score.Zero;

    /// <summary>
    /// SHA256 hash of canonical data for change detection
    /// </summary>
    public ContentHash ContentHash { get; private set; } = null!;

    /// <summary>
    /// Version for optimistic concurrency control
    /// </summary>
    public long Version { get; private set; }

    /// <summary>
    /// When the entity was first created
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core
    /// </summary>
    private ContentEntity()
    {
    }

    /// <summary>
    /// Factory method to create a new ContentEntity
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="type">Content type</param>
    /// <param name="title">Title</param>
    /// <param name="publishedAt">Publication date</param>
    /// <param name="categories">Categories list</param>
    /// <param name="sourceProvider">Provider identifier</param>
    /// <param name="metrics">Metrics as JSON</param>
    /// <param name="score">Calculated score</param>
    /// <param name="contentHash">Content hash for change detection</param>
    /// <returns>A new ContentEntity instance</returns>
    /// <exception cref="ArgumentException">Thrown when required fields are invalid</exception>
    public static ContentEntity Create(
        string id,
        ContentType type,
        string title,
        DateTimeOffset publishedAt,
        List<string> categories,
        string sourceProvider,
        JsonElement metrics,
        Score score,
        ContentHash contentHash)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id cannot be null or empty", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        }

        if (title.Length > 1000)
        {
            throw new ArgumentException("Title cannot exceed 1000 characters", nameof(title));
        }

        if (publishedAt > DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("PublishedAt cannot be in the future", nameof(publishedAt));
        }

        if (categories == null || categories.Count == 0)
        {
            throw new ArgumentException("Categories cannot be empty", nameof(categories));
        }

        if (string.IsNullOrWhiteSpace(sourceProvider))
        {
            throw new ArgumentException("SourceProvider cannot be null or empty", nameof(sourceProvider));
        }

        var now = DateTimeOffset.UtcNow;

        return new ContentEntity
        {
            Id = id,
            Type = type,
            Title = title,
            PublishedAt = publishedAt,
            Categories = categories,
            SourceProvider = sourceProvider,
            Metrics = metrics,
            Score = score,
            ContentHash = contentHash,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates the content with new data
    /// Used for upsert scenarios where content already exists
    /// </summary>
    /// <param name="title">New title</param>
    /// <param name="publishedAt">New publication date</param>
    /// <param name="categories">New categories</param>
    /// <param name="metrics">New metrics</param>
    /// <param name="score">New calculated score</param>
    /// <param name="contentHash">New content hash</param>
    public void Update(
        string title,
        DateTimeOffset publishedAt,
        List<string> categories,
        JsonElement metrics,
        Score score,
        ContentHash contentHash)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        }

        if (title.Length > 1000)
        {
            throw new ArgumentException("Title cannot exceed 1000 characters", nameof(title));
        }

        if (categories == null || categories.Count == 0)
        {
            throw new ArgumentException("Categories cannot be empty", nameof(categories));
        }

        Title = title;
        PublishedAt = publishedAt;
        Categories = categories;
        Metrics = metrics;
        Score = score;
        ContentHash = contentHash;
        UpdatedAt = DateTimeOffset.UtcNow;
        // Version will be incremented by trigger in DB
    }

    /// <summary>
    /// Updates only the score (used by Time Service for recency updates)
    /// </summary>
    /// <param name="newScore">The new score value</param>
    public void UpdateScore(Score newScore)
    {
        Score = newScore;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if this content has been modified by comparing hashes
    /// </summary>
    /// <param name="newHash">The hash to compare against</param>
    /// <returns>True if content has changed, false otherwise</returns>
    public bool HasChanged(ContentHash newHash)
    {
        return ContentHash != newHash;
    }

    /// <summary>
    /// Calculates days since publication (for recency score calculation)
    /// </summary>
    /// <returns>Number of days since publication</returns>
    public int DaysSincePublication()
    {
        return (int)(DateTimeOffset.UtcNow - PublishedAt).TotalDays;
    }
}
