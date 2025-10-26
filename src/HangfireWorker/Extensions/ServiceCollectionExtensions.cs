using Polly;
using Polly.Extensions.Http;
using SearchCase.HangfireWorker.Configuration;
using SearchCase.HangfireWorker.Services;

namespace SearchCase.HangfireWorker.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add microservice HTTP clients with resilience policies
    /// </summary>
    public static IServiceCollection AddMicroserviceClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(MicroserviceSettings.SectionName)
            .Get<MicroserviceSettings>() ?? new MicroserviceSettings();

        settings.Validate();

        // Register IMicroserviceClient
        services.AddScoped<IMicroserviceClient, MicroserviceClient>();

        // Add Service A HTTP Client with resilience
        services.AddHttpClient("ServiceA", client =>
        {
            client.BaseAddress = new Uri(settings.ServiceA.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.ServiceA.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "HangfireWorker/1.0");
            client.DefaultRequestHeaders.Add("X-Service-Name", "HangfireWorker");
        })
        .AddPolicyHandler(GetRetryPolicy(settings.ServiceA))
        .AddPolicyHandler(GetCircuitBreakerPolicy(settings.ServiceA));

        // Add Service B HTTP Client with resilience
        services.AddHttpClient("ServiceB", client =>
        {
            client.BaseAddress = new Uri(settings.ServiceB.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.ServiceB.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "HangfireWorker/1.0");
            client.DefaultRequestHeaders.Add("X-Service-Name", "HangfireWorker");
        })
        .AddPolicyHandler(GetRetryPolicy(settings.ServiceB))
        .AddPolicyHandler(GetCircuitBreakerPolicy(settings.ServiceB));

        return services;
    }

    /// <summary>
    /// Get retry policy with exponential backoff
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(MicroserviceConfig config)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: config.RetryCount,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(config.RetryDelaySeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Retry {RetryCount} after {Delay}ms. Reason: {Reason}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                });
    }

    /// <summary>
    /// Get circuit breaker policy
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(MicroserviceConfig config)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: config.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromSeconds(config.CircuitBreakerDurationSeconds),
                onBreak: (outcome, breakDuration, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogError(
                        "Circuit breaker opened for {Duration}s. Reason: {Reason}",
                        breakDuration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                },
                onReset: context =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Circuit breaker reset");
                });
    }

    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("Logger", out var logger))
            return logger as ILogger;
        return null;
    }
}
