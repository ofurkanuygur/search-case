using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Nest;
using SearchWorker.Data.Infrastructure;
using SearchWorker.Data.Mappers;
using SearchWorker.Data.Repositories;
using SearchWorker.Services;

namespace SearchWorker.Configuration;

/// <summary>
/// Extension methods for configuring services
/// Follows Open-Closed Principle - can be extended without modification
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Elasticsearch services to the DI container
    /// </summary>
    public static IServiceCollection AddElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // Register and validate options
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));
        services.AddSingleton<IValidateOptions<ElasticsearchOptions>, ElasticsearchOptionsValidator>();

        // Register Elasticsearch client
        services.AddSingleton<IElasticClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<IElasticClient>>();

            var uri = new Uri(options.Url);
            var settings = new ConnectionSettings(uri)
                .DefaultIndex(options.IndexName)
                .DefaultMappingFor<Models.SearchDocument>(m => m.IndexName(options.IndexName))
                .RequestTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds))
                .MaximumRetries(options.MaxRetries)
                .ThrowExceptions(false);

            // Add authentication if configured
            if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
            {
                settings.BasicAuthentication(options.Username, options.Password);
            }

            // Enable debug mode if configured
            if (options.EnableDebugMode)
            {
                settings.EnableDebugMode()
                    .PrettyJson()
                    .DisableDirectStreaming();
            }

            var client = new ElasticClient(settings);

            logger.LogInformation("Elasticsearch client configured: {Url}, Index: {IndexName}",
                options.Url,
                options.IndexName);

            return client;
        });

        // Register Elasticsearch service
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();

        return services;
    }

    /// <summary>
    /// Adds data access services to the DI container
    /// </summary>
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // Register and validate database options
        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("WriteServiceDb")
                ?? throw new InvalidOperationException("WriteServiceDb connection string not configured");
        });
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        // Register infrastructure
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

        // Register mappers
        services.AddSingleton<ISearchDocumentMapper, SearchDocumentMapper>();

        // Register repositories
        services.AddScoped<ISearchRepository, SearchRepository>();

        return services;
    }

    /// <summary>
    /// Initializes Elasticsearch index on startup
    /// </summary>
    public static async Task<IServiceProvider> InitializeElasticsearchAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IElasticsearchService>>();

        try
        {
            var exists = await elasticsearchService.IndexExistsAsync();
            if (!exists)
            {
                logger.LogInformation("Creating Elasticsearch index...");
                var created = await elasticsearchService.CreateIndexAsync();
                if (created)
                {
                    logger.LogInformation("Elasticsearch index created successfully");
                }
                else
                {
                    logger.LogWarning("Failed to create Elasticsearch index");
                }
            }
            else
            {
                logger.LogInformation("Elasticsearch index already exists");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Elasticsearch index");
        }

        return serviceProvider;
    }

    /// <summary>
    /// Options validator for Elasticsearch configuration
    /// </summary>
    private sealed class ElasticsearchOptionsValidator : IValidateOptions<ElasticsearchOptions>
    {
        public ValidateOptionsResult Validate(string? name, ElasticsearchOptions options)
        {
            try
            {
                options.Validate();
                return ValidateOptionsResult.Success;
            }
            catch (Exception ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
        }
    }

    /// <summary>
    /// Options validator for database configuration
    /// </summary>
    private sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
    {
        public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
        {
            try
            {
                options.Validate();
                return ValidateOptionsResult.Success;
            }
            catch (Exception ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
        }
    }
}
