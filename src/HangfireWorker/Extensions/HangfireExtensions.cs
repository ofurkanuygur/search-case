using Hangfire;
using Hangfire.PostgreSql;
using SearchCase.HangfireWorker.Configuration;
using SearchCase.HangfireWorker.Jobs;

namespace SearchCase.HangfireWorker.Extensions;

/// <summary>
/// Extension methods for Hangfire configuration
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Add Hangfire with PostgreSQL storage
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HangfireDb")
            ?? throw new InvalidOperationException("HangfireDb connection string is not configured");

        var settings = configuration
            .GetSection(HangfireSettings.SectionName)
            .Get<HangfireSettings>() ?? new HangfireSettings();

        settings.Validate();

        // Add Hangfire services
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                }, new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    TransactionSynchronisationTimeout = TimeSpan.FromMinutes(5),
                    InvisibilityTimeout = TimeSpan.FromMinutes(30),
                    DistributedLockTimeout = TimeSpan.FromMinutes(10)
                });
        });

        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = settings.WorkerCount;
            options.ServerName = $"{settings.ServerName}-{Environment.MachineName}";
            options.Queues = settings.Queues;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);
            options.ServerCheckInterval = TimeSpan.FromMinutes(1);
            options.ServerTimeout = TimeSpan.FromMinutes(5);
        });

        // Register job services
        services.AddScoped<FrequentJob>();
        services.AddScoped<DailyJob>();

        return services;
    }

    /// <summary>
    /// Configure recurring jobs
    /// </summary>
    public static IApplicationBuilder UseHangfireJobs(this IApplicationBuilder app)
    {
        // Schedule FrequentJob - every 5 minutes
        RecurringJob.AddOrUpdate<FrequentJob>(
            recurringJobId: "frequent-job",
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: "*/5 * * * *", // Every 5 minutes
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Schedule DailyJob - every day at 02:00 UTC
        RecurringJob.AddOrUpdate<DailyJob>(
            recurringJobId: "daily-job",
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: Cron.Daily(2, 0), // 02:00 UTC
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        return app;
    }

    /// <summary>
    /// Use Hangfire dashboard with authentication
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboardWithAuth(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(HangfireSettings.SectionName)
            .Get<HangfireSettings>() ?? new HangfireSettings();

        if (!settings.DashboardEnabled)
            return app;

        var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";

        app.UseHangfireDashboard(settings.DashboardPath, new DashboardOptions
        {
            AppPath = null,
            DashboardTitle = "SearchCase Hangfire Dashboard",
            StatsPollingInterval = 5000,
            DisplayStorageConnectionString = false,
            IsReadOnlyFunc = _ => false, // Set to true in production if needed
            Authorization = new[]
            {
                new Configuration.HangfireDashboardAuthorizationFilter(allowAnonymous: isDevelopment)
            }
        });

        return app;
    }
}
