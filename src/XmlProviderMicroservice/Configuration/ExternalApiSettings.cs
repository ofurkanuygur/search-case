namespace XmlProviderMicroservice.Configuration;

/// <summary>
/// Configuration settings for the external provider API
/// </summary>
public sealed class ExternalApiSettings
{
    public const string SectionName = "ExternalApi";

    /// <summary>
    /// Base URL of the external API
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retries in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Circuit breaker failure threshold
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker duration in seconds
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed logging for HTTP requests
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException($"{nameof(BaseUrl)} cannot be empty");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException($"{nameof(TimeoutSeconds)} must be greater than 0");

        if (RetryCount < 0)
            throw new InvalidOperationException($"{nameof(RetryCount)} cannot be negative");
    }
}
