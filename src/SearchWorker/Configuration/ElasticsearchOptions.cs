namespace SearchWorker.Configuration;

/// <summary>
/// Configuration options for Elasticsearch
/// Follows Options Pattern for configuration management
/// </summary>
public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "content-index";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int NumberOfShards { get; set; } = 1;
    public int NumberOfReplicas { get; set; } = 0;
    public int RefreshIntervalSeconds { get; set; } = 1;
    public bool EnableDebugMode { get; set; } = false;

    /// <summary>
    /// Validates configuration options
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Url))
            throw new InvalidOperationException("Elasticsearch URL is required");

        if (string.IsNullOrWhiteSpace(IndexName))
            throw new InvalidOperationException("Elasticsearch IndexName is required");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("TimeoutSeconds must be greater than 0");

        if (MaxRetries < 0)
            throw new InvalidOperationException("MaxRetries must be non-negative");

        if (NumberOfShards < 1)
            throw new InvalidOperationException("NumberOfShards must be at least 1");

        if (NumberOfReplicas < 0)
            throw new InvalidOperationException("NumberOfReplicas must be non-negative");
    }
}
