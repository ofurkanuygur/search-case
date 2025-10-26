using Serilog;
using SearchCase.HangfireWorker.Extensions;

// Configure Serilog early - minimal bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Hangfire Worker Service");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from configuration only (appsettings.json has all sink configurations)
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Add Hangfire with PostgreSQL
    builder.Services.AddHangfireConfiguration(builder.Configuration);

    // Add Microservice HTTP Clients with resilience
    builder.Services.AddMicroserviceClients(builder.Configuration);

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("HangfireDb")!,
            name: "postgresql",
            tags: new[] { "db", "ready" })
        .AddUrlGroup(
            new Uri($"{builder.Configuration["Microservices:ServiceA:BaseUrl"]}/health"),
            name: "microservice-a",
            tags: new[] { "external", "optional" },
            timeout: TimeSpan.FromSeconds(3),
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
        .AddUrlGroup(
            new Uri($"{builder.Configuration["Microservices:ServiceB:BaseUrl"]}/health"),
            name: "microservice-b",
            tags: new[] { "external", "optional" },
            timeout: TimeSpan.FromSeconds(3),
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    // Health check endpoints
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") // Only critical dependencies
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") // Same as /health
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Always returns healthy if app is running
    });
    app.MapHealthChecks("/health/external", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("external") // Optional external services
    });

    // Hangfire Dashboard
    app.UseHangfireDashboardWithAuth(builder.Configuration);

    // Configure recurring jobs
    app.UseHangfireJobs();

    Log.Information("Hangfire Worker Service configured successfully");
    Log.Information("Dashboard available at: /hangfire");
    Log.Information("Health checks: /health (critical), /health/ready, /health/live, /health/external (optional)");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
