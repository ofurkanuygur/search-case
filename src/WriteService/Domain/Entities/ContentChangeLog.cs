using WriteService.Domain.Enums;

namespace WriteService.Domain.Entities;

/// <summary>
/// Tracks changes to content items for auditing and history
/// </summary>
public sealed class ContentChangeLog
{
    /// <summary>
    /// Unique identifier for the change log entry
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The content ID that was changed
    /// </summary>
    public string ContentId { get; private set; }

    /// <summary>
    /// Type of change that occurred
    /// </summary>
    public ChangeType ChangeType { get; private set; }

    /// <summary>
    /// Previous content hash before the change
    /// </summary>
    public string? PreviousHash { get; private set; }

    /// <summary>
    /// New content hash after the change
    /// </summary>
    public string NewHash { get; private set; }

    /// <summary>
    /// JSON representation of changed fields
    /// </summary>
    public string? ChangedFields { get; private set; }

    /// <summary>
    /// Previous score value
    /// </summary>
    public decimal? PreviousScore { get; private set; }

    /// <summary>
    /// New score value
    /// </summary>
    public decimal NewScore { get; private set; }

    /// <summary>
    /// Source provider that triggered the change
    /// </summary>
    public string SourceProvider { get; private set; }

    /// <summary>
    /// Timestamp when the change was detected
    /// </summary>
    public DateTimeOffset DetectedAt { get; private set; }

    /// <summary>
    /// Sync batch identifier for grouping changes
    /// </summary>
    public Guid SyncBatchId { get; private set; }

    private ContentChangeLog()
    {
        ContentId = string.Empty;
        NewHash = string.Empty;
        SourceProvider = string.Empty;
    }

    /// <summary>
    /// Creates a new change log entry
    /// </summary>
    public static ContentChangeLog Create(
        string contentId,
        ChangeType changeType,
        string newHash,
        decimal newScore,
        string sourceProvider,
        Guid syncBatchId,
        string? previousHash = null,
        decimal? previousScore = null,
        string? changedFields = null)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("Content ID cannot be empty", nameof(contentId));

        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException("New hash cannot be empty", nameof(newHash));

        if (string.IsNullOrWhiteSpace(sourceProvider))
            throw new ArgumentException("Source provider cannot be empty", nameof(sourceProvider));

        if (syncBatchId == Guid.Empty)
            throw new ArgumentException("Sync batch ID cannot be empty", nameof(syncBatchId));

        return new ContentChangeLog
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            ChangeType = changeType,
            PreviousHash = previousHash,
            NewHash = newHash,
            ChangedFields = changedFields,
            PreviousScore = previousScore,
            NewScore = newScore,
            SourceProvider = sourceProvider,
            DetectedAt = DateTimeOffset.UtcNow,
            SyncBatchId = syncBatchId
        };
    }
}