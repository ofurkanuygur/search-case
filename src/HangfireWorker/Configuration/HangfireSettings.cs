namespace SearchCase.HangfireWorker.Configuration;

/// <summary>
/// Hangfire configuration settings
/// </summary>
public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Number of concurrent workers processing jobs
    /// </summary>
    public int WorkerCount { get; set; } = 5;

    /// <summary>
    /// Server name for identification in dashboard
    /// </summary>
    public string ServerName { get; set; } = "hangfire-worker";

    /// <summary>
    /// Queue names to process (in order of priority)
    /// </summary>
    public string[] Queues { get; set; } = new[] { "default" };

    /// <summary>
    /// Enable Hangfire dashboard
    /// </summary>
    public bool DashboardEnabled { get; set; } = true;

    /// <summary>
    /// Dashboard path
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// Job expiration time in days
    /// </summary>
    public int JobExpirationDays { get; set; } = 7;

    /// <summary>
    /// Validate configuration
    /// </summary>
    public void Validate()
    {
        if (WorkerCount <= 0)
            throw new InvalidOperationException($"{nameof(WorkerCount)} must be greater than 0");

        if (string.IsNullOrWhiteSpace(ServerName))
            throw new InvalidOperationException($"{nameof(ServerName)} cannot be empty");

        if (Queues == null || Queues.Length == 0)
            throw new InvalidOperationException($"{nameof(Queues)} must contain at least one queue");

        if (JobExpirationDays <= 0)
            throw new InvalidOperationException($"{nameof(JobExpirationDays)} must be greater than 0");
    }
}
