namespace WriteService.Domain.Entities;

/// <summary>
/// Represents a batch synchronization operation with metrics
/// </summary>
public sealed class SyncBatch
{
    /// <summary>
    /// Unique identifier for the sync batch
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// When the sync started
    /// </summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    /// When the sync completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Total items fetched from providers
    /// </summary>
    public int TotalItemsFetched { get; private set; }

    /// <summary>
    /// Items that were newly created
    /// </summary>
    public int ItemsCreated { get; private set; }

    /// <summary>
    /// Items that were updated
    /// </summary>
    public int ItemsUpdated { get; private set; }

    /// <summary>
    /// Items that had no changes
    /// </summary>
    public int ItemsUnchanged { get; private set; }

    /// <summary>
    /// Items that failed to process
    /// </summary>
    public int ItemsFailed { get; private set; }

    /// <summary>
    /// Duration of the sync operation in milliseconds
    /// </summary>
    public long? DurationMs { get; private set; }

    /// <summary>
    /// Average processing time per item in milliseconds
    /// </summary>
    public double? AvgItemProcessingMs { get; private set; }

    /// <summary>
    /// Source providers involved in this sync
    /// </summary>
    public List<string> SourceProviders { get; private set; } = new();

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Whether the sync completed successfully
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// Total database rows affected
    /// </summary>
    public int DatabaseRowsAffected { get; private set; }

    private SyncBatch()
    {
        SourceProviders = new List<string>();
    }

    /// <summary>
    /// Starts a new sync batch
    /// </summary>
    public static SyncBatch Start(List<string> sourceProviders)
    {
        if (sourceProviders == null || !sourceProviders.Any())
            throw new ArgumentException("At least one source provider is required", nameof(sourceProviders));

        return new SyncBatch
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            SourceProviders = sourceProviders,
            IsSuccessful = false
        };
    }

    /// <summary>
    /// Records items fetched from providers
    /// </summary>
    public void RecordItemsFetched(int count)
    {
        if (count < 0)
            throw new ArgumentException("Count cannot be negative", nameof(count));

        TotalItemsFetched = count;
    }

    /// <summary>
    /// Records change detection results
    /// </summary>
    public void RecordChangeResults(int created, int updated, int unchanged, int failed = 0)
    {
        if (created < 0 || updated < 0 || unchanged < 0 || failed < 0)
            throw new ArgumentException("Counts cannot be negative");

        ItemsCreated = created;
        ItemsUpdated = updated;
        ItemsUnchanged = unchanged;
        ItemsFailed = failed;
    }

    /// <summary>
    /// Records database operation results
    /// </summary>
    public void RecordDatabaseResults(int rowsAffected)
    {
        if (rowsAffected < 0)
            throw new ArgumentException("Rows affected cannot be negative", nameof(rowsAffected));

        DatabaseRowsAffected = rowsAffected;
    }

    /// <summary>
    /// Completes the sync batch successfully
    /// </summary>
    public void CompleteSuccessfully()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        IsSuccessful = true;
        CalculateMetrics();
    }

    /// <summary>
    /// Marks the sync batch as failed
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));

        CompletedAt = DateTimeOffset.UtcNow;
        IsSuccessful = false;
        ErrorMessage = errorMessage;
        CalculateMetrics();
    }

    private void CalculateMetrics()
    {
        if (CompletedAt.HasValue)
        {
            DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;

            if (TotalItemsFetched > 0 && DurationMs > 0)
            {
                AvgItemProcessingMs = (double)DurationMs / TotalItemsFetched;
            }
        }
    }

    /// <summary>
    /// Gets a summary of the sync operation
    /// </summary>
    public string GetSummary()
    {
        return $"Sync Batch {Id}: " +
               $"Fetched={TotalItemsFetched}, " +
               $"Created={ItemsCreated}, " +
               $"Updated={ItemsUpdated}, " +
               $"Unchanged={ItemsUnchanged}, " +
               $"Failed={ItemsFailed}, " +
               $"Duration={DurationMs}ms, " +
               $"Success={IsSuccessful}";
    }
}