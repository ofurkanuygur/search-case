namespace SearchCase.Search.Contracts.Models;

/// <summary>
/// Search metadata with diagnostics
/// </summary>
public sealed record SearchMetadata
{
    /// <summary>
    /// Strategy name used for this search
    /// </summary>
    public string Strategy { get; init; } = default!;

    /// <summary>
    /// Data source (Redis, Elasticsearch, Hybrid)
    /// </summary>
    public string DataSource { get; init; } = default!;

    /// <summary>
    /// Query latency in milliseconds
    /// </summary>
    public long LatencyMs { get; init; }

    /// <summary>
    /// Result served from cache
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    /// Timestamp when search was executed
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
