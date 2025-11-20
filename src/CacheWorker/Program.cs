using CacheWorker.Consumers;
using CacheWorker.Extensions;
using CacheWorker.Services;
using MassTransit;
using Serilog;
using StackExchange.Redis;
using EventBusContracts;

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

    // Configure MassTransit with Kafka
    builder.Services.AddMassTransit(x =>
    {
        x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

        x.AddRider(rider =>
        {
            rider.AddConsumer<ContentBatchUpdatedConsumer>();

            rider.UsingKafka((context, k) =>
            {
                var kafkaHost = builder.Configuration["Kafka:Host"] ?? "localhost:9092";
                var topicName = builder.Configuration["Kafka:Topic"] ?? "content-events";
                var consumerGroup = builder.Configuration["Kafka:ConsumerGroup"] ?? "cache-worker-group";

                k.Host(kafkaHost);

                k.TopicEndpoint<ContentBatchUpdatedEvent>(topicName, consumerGroup, e =>
                {
                    e.ConfigureConsumer<ContentBatchUpdatedConsumer>(context);

                    // Resilience Patterns
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));
                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.TripThreshold = 5;
                        cb.ActiveThreshold = 2;
                        cb.ResetInterval = TimeSpan.FromMinutes(1);
                    });
                });
            });
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
        tags: new[] { "cache", "redis" })
    .AddKafka(
        new Confluent.Kafka.ProducerConfig { BootstrapServers = builder.Configuration["Kafka:Host"] ?? "localhost:9092" },
        name: "kafka",
        tags: new[] { "messaging", "ready" });

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
app.Logger.LogInformation("Kafka Host: {Host}",
    builder.Configuration["Kafka:Host"]);

app.Run();