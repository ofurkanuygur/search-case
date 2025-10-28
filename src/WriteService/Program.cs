using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using WriteService.Extensions;
using WriteService.Infrastructure.Jobs;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Write Service");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/writeservice-.txt", rollingInterval: RollingInterval.Day));

    // Add Infrastructure (DB, Repositories, Services, HTTP Clients)
    builder.Services.AddWriteServiceInfrastructure(builder.Configuration);

    // Add Hangfire
    var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireDb");
    builder.Services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
        config.UseSimpleAssemblyNameTypeSerializer();
        config.UseRecommendedSerializerSettings();
        config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(hangfireConnectionString));
    });

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount", 2);
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("WriteServiceDb")!,
            name: "writeservice-db",
            tags: new[] { "db", "ready" })
        .AddNpgSql(
            hangfireConnectionString!,
            name: "hangfire-db",
            tags: new[] { "db", "ready" });

    var app = builder.Build();

    // Configure middleware
    app.UseSerilogRequestLogging();

    // Hangfire Dashboard
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
    });

    // Health check endpoints
    app.MapHealthChecks("/health");

    // Schedule Hangfire jobs
    var cronExpression = builder.Configuration.GetValue<string>("Hangfire:SyncJobCronExpression", "*/5 * * * *");

    RecurringJob.AddOrUpdate<ContentSyncJob>(
        "content-sync-job",
        job => job.ExecuteAsync(CancellationToken.None),
        cronExpression);

    // Schedule Freshness Score Update Job - Daily at 02:00 UTC
    RecurringJob.AddOrUpdate<FreshnessScoreUpdateJob>(
        "freshness-score-update-job",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 2 * * *"); // Daily at 02:00 UTC

    Log.Information("Write Service started successfully");
    Log.Information("Hangfire Dashboard: /hangfire");
    Log.Information("Content Sync Job scheduled: {Cron}", cronExpression);
    Log.Information("Freshness Score Update Job scheduled: Daily at 02:00 UTC");

    // Run ContentSyncJob once on startup for testing
    Log.Information("Running ContentSyncJob on startup...");
    using (var scope = app.Services.CreateScope())
    {
        var syncJob = scope.ServiceProvider.GetRequiredService<ContentSyncJob>();
        await syncJob.ExecuteAsync();
    }
    Log.Information("ContentSyncJob completed");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Write Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Allow all for dashboard in development
file class AllowAllDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
