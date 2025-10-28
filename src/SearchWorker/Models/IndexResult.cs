namespace SearchWorker.Models;

/// <summary>
/// Result of batch indexing operation
/// </summary>
public sealed class IndexResult
{
    public bool IsSuccess { get; init; }
    public int TotalProcessed { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<string> FailedIds { get; init; } = new();
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }

    public static IndexResult Success(int count, TimeSpan duration)
    {
        return new IndexResult
        {
            IsSuccess = true,
            TotalProcessed = count,
            SuccessCount = count,
            FailedCount = 0,
            Duration = duration
        };
    }

    public static IndexResult PartialSuccess(int total, int success, List<string> failedIds, TimeSpan duration)
    {
        return new IndexResult
        {
            IsSuccess = success > 0,
            TotalProcessed = total,
            SuccessCount = success,
            FailedCount = failedIds.Count,
            FailedIds = failedIds,
            Duration = duration
        };
    }

    public static IndexResult Failure(int total, List<string> failedIds, TimeSpan duration, string errorMessage)
    {
        return new IndexResult
        {
            IsSuccess = false,
            TotalProcessed = total,
            SuccessCount = 0,
            FailedCount = failedIds.Count,
            FailedIds = failedIds,
            Duration = duration,
            ErrorMessage = errorMessage
        };
    }
}
