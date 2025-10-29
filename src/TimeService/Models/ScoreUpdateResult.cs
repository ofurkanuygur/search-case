namespace TimeService.Models;

/// <summary>
/// Result of score update operation
/// Contains statistics and metadata about the update process
/// </summary>
public sealed class ScoreUpdateResult
{
    /// <summary>
    /// Number of content items processed
    /// </summary>
    public int TotalProcessed { get; init; }

    /// <summary>
    /// Number of content items actually updated (scores changed)
    /// </summary>
    public int Updated { get; init; }

    /// <summary>
    /// Number of content items skipped (scores unchanged)
    /// </summary>
    public int Skipped { get; init; }

    /// <summary>
    /// Number of errors encountered
    /// </summary>
    public int Errors { get; init; }

    /// <summary>
    /// How long the operation took
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When the update was started
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// When the update completed
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// List of content IDs that were updated
    /// Used for event publishing to notify downstream services
    /// </summary>
    public List<string> UpdatedContentIds { get; init; } = new();

    /// <summary>
    /// Whether the operation was successful overall
    /// </summary>
    public bool IsSuccess => Errors == 0 && Updated > 0;

    /// <summary>
    /// Human-readable summary of the operation
    /// </summary>
    public string GetSummary()
    {
        return $"Score update completed: {Updated} updated, {Skipped} skipped, " +
               $"{Errors} errors (Total: {TotalProcessed}, Duration: {Duration.TotalSeconds:F2}s)";
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ScoreUpdateResult Success(
        int totalProcessed,
        int updated,
        int skipped,
        List<string> updatedContentIds,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt)
    {
        return new ScoreUpdateResult
        {
            TotalProcessed = totalProcessed,
            Updated = updated,
            Skipped = skipped,
            Errors = 0,
            UpdatedContentIds = updatedContentIds,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Duration = completedAt - startedAt
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static ScoreUpdateResult Failed(
        int totalProcessed,
        int updated,
        int errors,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt)
    {
        return new ScoreUpdateResult
        {
            TotalProcessed = totalProcessed,
            Updated = updated,
            Skipped = 0,
            Errors = errors,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Duration = completedAt - startedAt
        };
    }
}
