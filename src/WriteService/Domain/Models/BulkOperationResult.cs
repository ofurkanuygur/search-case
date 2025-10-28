namespace WriteService.Domain.Models;

/// <summary>
/// Result of a bulk database operation
/// </summary>
public sealed class BulkOperationResult
{
    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int TotalProcessed { get; }

    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailureCount { get; }

    /// <summary>
    /// Number of rows affected in the database
    /// </summary>
    public int RowsAffected { get; }

    /// <summary>
    /// Time taken for the operation in milliseconds
    /// </summary>
    public long ElapsedMs { get; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess => FailureCount == 0 && TotalProcessed > 0;

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalProcessed > 0
        ? (double)SuccessCount / TotalProcessed * 100
        : 0;

    /// <summary>
    /// Average time per item in milliseconds
    /// </summary>
    public double AvgTimePerItem => TotalProcessed > 0
        ? (double)ElapsedMs / TotalProcessed
        : 0;

    /// <summary>
    /// List of errors that occurred
    /// </summary>
    public List<BulkOperationError> Errors { get; }

    /// <summary>
    /// IDs of successfully processed items
    /// </summary>
    public List<string> SuccessfulIds { get; }

    /// <summary>
    /// IDs of failed items
    /// </summary>
    public List<string> FailedIds { get; }

    public BulkOperationResult(
        int totalProcessed,
        int successCount,
        int failureCount,
        int rowsAffected,
        long elapsedMs,
        List<string>? successfulIds = null,
        List<string>? failedIds = null,
        List<BulkOperationError>? errors = null)
    {
        if (totalProcessed < 0 || successCount < 0 || failureCount < 0 || rowsAffected < 0)
            throw new ArgumentException("Counts cannot be negative");

        if (successCount + failureCount > totalProcessed)
            throw new ArgumentException("Success + Failure count cannot exceed total processed");

        TotalProcessed = totalProcessed;
        SuccessCount = successCount;
        FailureCount = failureCount;
        RowsAffected = rowsAffected;
        ElapsedMs = elapsedMs;
        SuccessfulIds = successfulIds ?? new List<string>();
        FailedIds = failedIds ?? new List<string>();
        Errors = errors ?? new List<BulkOperationError>();
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static BulkOperationResult Success(
        int totalProcessed,
        int rowsAffected,
        long elapsedMs,
        List<string>? successfulIds = null)
    {
        return new BulkOperationResult(
            totalProcessed,
            totalProcessed,
            0,
            rowsAffected,
            elapsedMs,
            successfulIds);
    }

    /// <summary>
    /// Creates a partial success result
    /// </summary>
    public static BulkOperationResult PartialSuccess(
        int totalProcessed,
        int successCount,
        int failureCount,
        int rowsAffected,
        long elapsedMs,
        List<string>? successfulIds = null,
        List<string>? failedIds = null,
        List<BulkOperationError>? errors = null)
    {
        return new BulkOperationResult(
            totalProcessed,
            successCount,
            failureCount,
            rowsAffected,
            elapsedMs,
            successfulIds,
            failedIds,
            errors);
    }

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static BulkOperationResult Failure(
        int totalProcessed,
        long elapsedMs,
        List<string>? failedIds = null,
        List<BulkOperationError>? errors = null)
    {
        return new BulkOperationResult(
            totalProcessed,
            0,
            totalProcessed,
            0,
            elapsedMs,
            null,
            failedIds,
            errors);
    }

    /// <summary>
    /// Gets a summary of the operation
    /// </summary>
    public string GetSummary()
    {
        var status = IsSuccess ? "Success" : FailureCount == TotalProcessed ? "Failed" : "Partial Success";
        return $"Bulk Operation {status}: " +
               $"Processed={TotalProcessed}, " +
               $"Success={SuccessCount}, " +
               $"Failed={FailureCount}, " +
               $"Rows={RowsAffected}, " +
               $"Time={ElapsedMs}ms, " +
               $"Rate={SuccessRate:F1}%";
    }
}

/// <summary>
/// Represents an error in a bulk operation
/// </summary>
public sealed class BulkOperationError
{
    /// <summary>
    /// ID of the item that failed
    /// </summary>
    public string ItemId { get; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional exception details
    /// </summary>
    public string? ExceptionDetails { get; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTimeOffset OccurredAt { get; }

    public BulkOperationError(
        string itemId,
        string message,
        string? exceptionDetails = null)
    {
        ItemId = itemId ?? throw new ArgumentNullException(nameof(itemId));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ExceptionDetails = exceptionDetails;
        OccurredAt = DateTimeOffset.UtcNow;
    }
}