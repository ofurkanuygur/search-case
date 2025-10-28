using CacheWorker.Consumers;
using CacheWorker.Extensions;
using CacheWorker.Services;
using MassTransit;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/cacheworker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "CacheWorker API", Version = "v1" });
});

// Register data access services using SOLID principles
builder.Services.AddDataAccess(builder.Configuration);

// Register cache service
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<ContentBatchUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        var host = rabbitMqConfig["Host"] ?? "localhost";
        var username = rabbitMqConfig["Username"] ?? "guest";
        var password = rabbitMqConfig["Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Configure consumer endpoint with retry policy
        cfg.ReceiveEndpoint("cache-worker-queue", e =>
        {
            e.ConfigureConsumer<ContentBatchUpdatedConsumer>(context);

            // Retry policy - exponential backoff
            e.UseMessageRetry(r => r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(5),
                maxInterval: TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(5)));

            // Circuit breaker
            e.UseCircuitBreaker(cb =>
            {
                cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                cb.TripThreshold = 5;
                cb.ActiveThreshold = 2;
                cb.ResetInterval = TimeSpan.FromMinutes(5);
            });

            // Concurrency limit
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 4;
        });

        // Configure timeout
        cfg.ConfigureEndpoints(context);
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("WriteServiceDb") ?? "",
        name: "postgresql-db",
        tags: new[] { "db", "postgresql" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis-cache",
        tags: new[] { "cache", "redis" });
    // RabbitMQ health check removed - MassTransit manages its own connection

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache")
});
app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});

// Startup logging
app.Logger.LogInformation("CacheWorker starting up");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("PostgreSQL Connection: {Connection}",
    builder.Configuration.GetConnectionString("WriteServiceDb"));
app.Logger.LogInformation("Redis Connection: {Connection}",
    builder.Configuration.GetConnectionString("Redis"));
app.Logger.LogInformation("RabbitMQ Host: {Host}",
    builder.Configuration["RabbitMQ:Host"]);

app.Run();