namespace CacheWorker.Models;

/// <summary>
/// Result of cache update operations
/// </summary>
public class CacheResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedIds { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public static CacheResult Success(int count, TimeSpan duration)
    {
        return new CacheResult
        {
            TotalProcessed = count,
            SuccessCount = count,
            FailedCount = 0,
            Duration = duration
        };
    }

    public static CacheResult PartialSuccess(int total, int success, List<string> failedIds, TimeSpan duration)
    {
        return new CacheResult
        {
            TotalProcessed = total,
            SuccessCount = success,
            FailedCount = failedIds.Count,
            FailedIds = failedIds,
            Duration = duration
        };
    }

    public static CacheResult Failure(int total, List<string> failedIds, TimeSpan duration)
    {
        return new CacheResult
        {
            TotalProcessed = total,
            SuccessCount = 0,
            FailedCount = failedIds.Count,
            FailedIds = failedIds,
            Duration = duration
        };
    }
}