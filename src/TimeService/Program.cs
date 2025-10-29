using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using TimeService.Configuration;
using TimeService.Infrastructure.Repository;
using TimeService.Services.Calculation;
using TimeService.Services.EventBus;
using TimeService.Services.Orchestration;
using TimeService.Data;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TimeService");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Database - TimeService's own DbContext (shared database schema)
    var connectionString = builder.Configuration.GetConnectionString("WriteServiceDb");
    builder.Services.AddDbContext<TimeServiceDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        options.UseSnakeCaseNamingConvention();
    });

    // Calculation Services (SOLID: Strategy Pattern)
    builder.Services.AddSingleton<IFreshnessCalculator, FreshnessCalculator>();
    builder.Services.AddSingleton<IScoreCalculationStrategy, VideoScoreStrategy>();
    builder.Services.AddSingleton<IScoreCalculationStrategy, ArticleScoreStrategy>();

    // Repository
    builder.Services.AddScoped<IScoreRepository, ScoreRepository>();

    // Orchestrator
    builder.Services.AddScoped<ITimeServiceOrchestrator, TimeServiceOrchestrator>();

    // EventBus HTTP Client with Polly resilience
    var eventBusSettings = builder.Configuration
        .GetSection(EventBusSettings.SectionName)
        .Get<EventBusSettings>() ?? new EventBusSettings();

    eventBusSettings.Validate();

    builder.Services.AddHttpClient<IEventBusClient, EventBusClient>(client =>
    {
        client.BaseAddress = new Uri(eventBusSettings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(eventBusSettings.TimeoutSeconds);
        client.DefaultRequestHeaders.Add("User-Agent", "TimeService/1.0");
        client.DefaultRequestHeaders.Add("X-Service-Name", "TimeService");
    })
    .AddPolicyHandler(GetRetryPolicy(eventBusSettings))
    .AddPolicyHandler(GetCircuitBreakerPolicy(eventBusSettings));

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString!,
            name: "postgresql",
            tags: new[] { "db", "ready" });

    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    // Swagger (always enabled for TimeService)
    app.UseSwagger();
    app.UseSwaggerUI();

    // Health check endpoints
    app.MapHealthChecks("/health");

    app.MapControllers();

    Log.Information("TimeService configured successfully");
    Log.Information("Endpoints available:");
    Log.Information("  POST /api/time/update-daily - Update scores for threshold-crossing content");
    Log.Information("  POST /api/time/recalculate-all - Force recalculation of all scores");
    Log.Information("  GET /api/time/health - Health check");
    Log.Information("  GET /swagger - API documentation");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TimeService terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Polly retry policy with exponential backoff
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(EventBusSettings settings)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: settings.RetryCount,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(settings.RetryDelaySeconds, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning(
                    "EventBus request retry {RetryCount} after {Delay}ms. Reason: {Reason}",
                    retryCount,
                    timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            });
}

// Polly circuit breaker policy
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(EventBusSettings settings)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: settings.CircuitBreakerThreshold,
            durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakerDurationSeconds),
            onBreak: (outcome, breakDuration) =>
            {
                Log.Error(
                    "EventBus circuit breaker opened for {Duration}s. Reason: {Reason}",
                    breakDuration.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            },
            onReset: () =>
            {
                Log.Information("EventBus circuit breaker reset");
            });
}
