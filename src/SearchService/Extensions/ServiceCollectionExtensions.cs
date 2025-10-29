using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FluentValidation;
using SearchCase.Search.Contracts.Validators;
using SearchService.Clients.Elasticsearch;
using SearchService.Clients.Redis;
using SearchService.Configuration;
using SearchService.Orchestration;
using SearchService.Strategies;
using StackExchange.Redis;

namespace SearchService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        services.Configure<ElasticsearchSettings>(configuration.GetSection(ElasticsearchSettings.SectionName));

        // Validators
        services.AddValidatorsFromAssemblyContaining<SearchRequestValidator>();

        // Redis client
        services.AddRedisClient(configuration);

        // Elasticsearch client
        services.AddElasticsearchClient(configuration);

        // Search clients
        services.AddScoped<IRedisSearchClient, RedisSearchClient>();
        services.AddScoped<IElasticsearchSearchClient>(provider =>
        {
            var client = provider.GetRequiredService<ElasticsearchClient>();
            var logger = provider.GetRequiredService<ILogger<ElasticsearchSearchClient>>();
            var settings = configuration.GetSection(ElasticsearchSettings.SectionName)
                .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

            return new ElasticsearchSearchClient(client, logger, settings.IndexName);
        });

        // Search strategies
        services.AddScoped<ISearchStrategy, RedisSearchStrategy>();
        services.AddScoped<ISearchStrategy, ElasticsearchSearchStrategy>();
        services.AddScoped<ISearchStrategy, HybridSearchStrategy>();

        // Orchestrator
        services.AddScoped<ISearchOrchestrator, SearchOrchestrator>();

        return services;
    }

    private static IServiceCollection AddRedisClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName)
            .Get<RedisSettings>() ?? new RedisSettings();

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            options.ConnectTimeout = redisSettings.ConnectTimeout;
            options.SyncTimeout = redisSettings.SyncTimeout;
            options.AbortOnConnectFail = false;
            options.DefaultDatabase = redisSettings.DefaultDatabase;

            return ConnectionMultiplexer.Connect(options);
        });

        return services;
    }

    private static IServiceCollection AddElasticsearchClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var elasticsearchSettings = configuration.GetSection(ElasticsearchSettings.SectionName)
            .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

        services.AddSingleton<ElasticsearchClient>(provider =>
        {
            var settings = new ElasticsearchClientSettings(new Uri(elasticsearchSettings.Uri))
                .RequestTimeout(TimeSpan.FromSeconds(elasticsearchSettings.RequestTimeout))
                .EnableDebugMode()
                .PrettyJson();

            // Add authentication if configured
            if (!string.IsNullOrWhiteSpace(elasticsearchSettings.Username) &&
                !string.IsNullOrWhiteSpace(elasticsearchSettings.Password))
            {
                settings.Authentication(new BasicAuthentication(
                    elasticsearchSettings.Username,
                    elasticsearchSettings.Password));
            }

            return new ElasticsearchClient(settings);
        });

        return services;
    }
}
