namespace SearchCase.HangfireWorker.Configuration;

/// <summary>
/// Microservices configuration
/// </summary>
public sealed class MicroserviceSettings
{
    public const string SectionName = "Microservices";

    public MicroserviceConfig ServiceA { get; set; } = new();
    public MicroserviceConfig ServiceB { get; set; } = new();

    public void Validate()
    {
        ServiceA.Validate(nameof(ServiceA));
        ServiceB.Validate(nameof(ServiceB));
    }
}

/// <summary>
/// Individual microservice configuration
/// </summary>
public sealed class MicroserviceConfig
{
    /// <summary>
    /// Base URL of the microservice
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Initial retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Circuit breaker failure threshold
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker duration in seconds
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    public void Validate(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException($"{serviceName}.{nameof(BaseUrl)} cannot be empty");

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"{serviceName}.{nameof(BaseUrl)} must be a valid URL");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException($"{serviceName}.{nameof(TimeoutSeconds)} must be greater than 0");

        if (RetryCount < 0)
            throw new InvalidOperationException($"{serviceName}.{nameof(RetryCount)} cannot be negative");

        if (RetryDelaySeconds < 0)
            throw new InvalidOperationException($"{serviceName}.{nameof(RetryDelaySeconds)} cannot be negative");
    }
}
