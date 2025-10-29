namespace SearchWorker.Configuration;

/// <summary>
/// Configuration options for PostgreSQL database
/// Follows Options Pattern for configuration management
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Validates configuration options
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("Database ConnectionString is required");

        if (CommandTimeoutSeconds <= 0)
            throw new InvalidOperationException("CommandTimeoutSeconds must be greater than 0");

        if (MaxRetries < 0)
            throw new InvalidOperationException("MaxRetries must be non-negative");
    }
}
