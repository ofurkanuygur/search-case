using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using WriteService.Application.Interfaces;
using WriteService.Application.Services;
using WriteService.Configuration;
using WriteService.Data;
using WriteService.Data.Repositories;
using WriteService.Infrastructure.Jobs;
using WriteService.Infrastructure.EventBus;
using WriteService.Infrastructure.Scoring;

namespace WriteService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWriteServiceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<ProviderSettings>(configuration.GetSection(ProviderSettings.SectionName));

        // Database
        services.AddDbContext<WriteServiceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("WriteServiceDb");
            options.UseNpgsql(connectionString);
        });

        // Repositories
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IBulkOperationRepository, BulkOperationRepository>();

        // Application Services - Core
        services.AddScoped<IProviderClient, ProviderClient>();
        services.AddScoped<IChangeDetectionService, ChangeDetectionService>();
        services.AddScoped<IScoreCalculationService, ScoreCalculationService>();

        // Scoring Service (Case-specific formula)
        services.AddScoped<IScoringService, CaseScoringService>();

        // Application Services - Change Detection (Strategy Pattern)
        services.AddScoped<IChangeDetectionStrategy, HashBasedChangeDetectionStrategy>();

        // Application Services - Orchestration
        services.AddScoped<ContentSyncOrchestrator>(provider =>
        {
            var changeDetectionStrategy = provider.GetRequiredService<IChangeDetectionStrategy>();
            var bulkRepository = provider.GetRequiredService<IBulkOperationRepository>();
            var contentRepository = provider.GetRequiredService<IContentRepository>();
            var logger = provider.GetRequiredService<ILogger<ContentSyncOrchestrator>>();
            var eventBusClient = provider.GetRequiredService<IEventBusClient>(); // Now required

            return new ContentSyncOrchestrator(
                changeDetectionStrategy,
                bulkRepository,
                contentRepository,
                logger,
                eventBusClient);
        });

        // Jobs
        services.AddScoped<ContentSyncJob>();
        services.AddScoped<FreshnessScoreUpdateJob>(); // Legacy - kept for backward compatibility
        services.AddScoped<ContentScoreUpdateJob>(); // New: Full score recalculation with case formula

        // HTTP Clients with Polly
        var providerSettings = configuration.GetSection(ProviderSettings.SectionName).Get<ProviderSettings>()
            ?? new ProviderSettings();

        services.AddHttpClient("JsonProvider", client =>
        {
            client.BaseAddress = new Uri(providerSettings.JsonProvider.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(providerSettings.JsonProvider.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient("XmlProvider", client =>
        {
            client.BaseAddress = new Uri(providerSettings.XmlProvider.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(providerSettings.XmlProvider.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // EventBus Client with Circuit Breaker
        services.AddHttpClient<IEventBusClient, EventBusClient>("EventBus", client =>
        {
            var eventBusUrl = configuration["EventBus:BaseUrl"] ?? "http://localhost:5200";
            client.BaseAddress = new Uri(eventBusUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "WriteService");
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts if needed
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalMilliseconds}ms");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (result, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}
