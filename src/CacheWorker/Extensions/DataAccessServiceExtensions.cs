using CacheWorker.Data.Infrastructure;
using CacheWorker.Data.Mappers;
using CacheWorker.Data.Repositories;
using CacheWorker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CacheWorker.Extensions;

/// <summary>
/// Extension methods for configuring data access services
/// Follows Open-Closed Principle - can be extended without modification
/// </summary>
public static class DataAccessServiceExtensions
{
    /// <summary>
    /// Adds data access services to the DI container
    /// </summary>
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // Register configuration
        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("WriteServiceDb")
                ?? throw new InvalidOperationException("WriteServiceDb connection string not configured");
        });

        // Validate options at startup
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        // Register infrastructure
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

        // Register mappers
        services.AddSingleton<IContentMapper, ContentMapper>();

        // Register repositories
        services.AddScoped<IContentRepository, ContentRepository>();

        return services;
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