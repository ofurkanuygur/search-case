namespace TimeService.Configuration;

/// <summary>
/// EventBus configuration settings
/// </summary>
public sealed class EventBusSettings
{
    public const string SectionName = "EventBus";

    /// <summary>
    /// EventBusService base URL
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8004";

    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retries in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Circuit breaker threshold (failures before opening)
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 3;

    /// <summary>
    /// Circuit breaker open duration in seconds
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("EventBus:BaseUrl is required");
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"EventBus:BaseUrl is not a valid URL: {BaseUrl}");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("EventBus:TimeoutSeconds must be positive");
        }

        if (RetryCount < 0)
        {
            throw new InvalidOperationException("EventBus:RetryCount cannot be negative");
        }
    }
}
