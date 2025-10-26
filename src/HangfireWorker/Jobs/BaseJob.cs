using Hangfire;

namespace SearchCase.HangfireWorker.Jobs;

/// <summary>
/// Base class for all Hangfire jobs
/// Provides common functionality and error handling
/// </summary>
public abstract class BaseJob
{
    protected readonly ILogger Logger;

    protected BaseJob(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute the job with error handling and logging
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobName = GetType().Name;
        var startTime = DateTime.UtcNow;

        Logger.LogInformation(
            "Starting job {JobName} at {StartTime}",
            jobName,
            startTime);

        try
        {
            await ExecuteJobAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation(
                "Job {JobName} completed successfully. Duration: {Duration}ms",
                jobName,
                duration.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning(
                "Job {JobName} was cancelled",
                jobName);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError(
                ex,
                "Job {JobName} failed after {Duration}ms. Error: {ErrorMessage}",
                jobName,
                duration.TotalMilliseconds,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Implement job-specific logic
    /// </summary>
    protected abstract Task ExecuteJobAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get job display name for logging
    /// </summary>
    protected virtual string GetJobDisplayName() => GetType().Name;
}
