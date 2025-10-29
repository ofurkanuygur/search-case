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

    // Add HTTP client for test endpoints
    builder.Services.AddHttpClient();

    // Add Controllers for test endpoints
    builder.Services.AddControllers();

    // NOTE: Hangfire disabled - WriteService is now API-only
    // Jobs are orchestrated by HangfireWorker via HTTP endpoints
    // Uncomment below to enable Hangfire in WriteService:

    // var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireDb");
    // builder.Services.AddHangfire(config =>
    // {
    //     config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
    //     config.UseSimpleAssemblyNameTypeSerializer();
    //     config.UseRecommendedSerializerSettings();
    //     config.UsePostgreSqlStorage(options =>
    //         options.UseNpgsqlConnection(hangfireConnectionString));
    // });
    //
    // builder.Services.AddHangfireServer(options =>
    // {
    //     options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount", 2);
    // });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("WriteServiceDb")!,
            name: "writeservice-db",
            tags: new[] { "db", "ready" });

    var app = builder.Build();

    // Configure middleware
    app.UseSerilogRequestLogging();

    // Health check endpoints
    app.MapHealthChecks("/health");

    // Map controllers (for TestController and API endpoints)
    app.MapControllers();

    // NOTE: Hangfire Dashboard disabled - Jobs orchestrated by HangfireWorker
    // app.UseHangfireDashboard("/hangfire", new DashboardOptions
    // {
    //     Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
    // });

    Log.Information("Write Service started successfully (API-only mode)");
    Log.Information("Jobs are orchestrated by HangfireWorker via HTTP endpoints");
    Log.Information("Available endpoints:");
    Log.Information("  POST /api/content/sync - Trigger content synchronization");
    Log.Information("  GET /health - Health check");

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
