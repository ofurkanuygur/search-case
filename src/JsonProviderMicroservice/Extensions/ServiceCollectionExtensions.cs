using FluentValidation;
using JsonProviderMicroservice.Configuration;
using JsonProviderMicroservice.HttpClients;
using JsonProviderMicroservice.Mapping;
using JsonProviderMicroservice.Models;
using JsonProviderMicroservice.Services;
using Polly;
using Polly.Extensions.Http;
using SearchCase.Contracts.Mapping;
using SearchCase.Contracts.Validators;

namespace JsonProviderMicroservice.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds external API client with resilience patterns
    /// </summary>
    public static IServiceCollection AddExternalApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind settings
        var settings = configuration
            .GetSection(ExternalApiSettings.SectionName)
            .Get<ExternalApiSettings>() ?? new ExternalApiSettings();

        settings.Validate();

        services.Configure<ExternalApiSettings>(
            configuration.GetSection(ExternalApiSettings.SectionName));

        // Add HttpClient with Polly policies
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings))
        .AddPolicyHandler(GetCircuitBreakerPolicy(settings));

        return services;
    }

    /// <summary>
    /// Adds application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Add services
        services.AddScoped<IProviderService, ProviderService>();

        // Add mapper
        services.AddScoped<IContentMapper<Content>, JsonToCanonicalMapper>();

        // Add validators
        services.AddScoped<IValidator<SearchCase.Contracts.Models.CanonicalVideoContent>, CanonicalVideoContentValidator>();

        return services;
    }

    /// <summary>
    /// Gets retry policy with exponential backoff
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ExternalApiSettings settings)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: settings.RetryCount,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(settings.RetryDelaySeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine(
                        $"[Retry {retryCount}] Waiting {timespan.TotalSeconds}s before next retry. " +
                        $"Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                });
    }

    /// <summary>
    /// Gets circuit breaker policy
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ExternalApiSettings settings)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: settings.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakerDurationSeconds),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine(
                        $"[Circuit Breaker] Opened for {duration.TotalSeconds}s. " +
                        $"Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.WriteLine("[Circuit Breaker] Reset - back to normal operation");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("[Circuit Breaker] Half-Open - testing if service recovered");
                });
    }
}
