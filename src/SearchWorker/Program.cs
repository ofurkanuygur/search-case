using MassTransit;
using SearchWorker.Configuration;
using SearchWorker.Consumers;
using Serilog;
using EventBusContracts;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/searchworker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SearchWorker API", Version = "v1" });
});

// Register Elasticsearch services
builder.Services.AddElasticsearch(builder.Configuration);

// Register data access services
builder.Services.AddDataAccess(builder.Configuration);

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
            var consumerGroup = builder.Configuration["Kafka:ConsumerGroup"] ?? "search-worker-group";

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

                // Concurrency limit
                e.PrefetchCount = 16;
                e.ConcurrentMessageLimit = 4;
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
    .AddElasticsearch(
        builder.Configuration.GetSection("Elasticsearch:Url").Value ?? "http://localhost:9200",
        name: "elasticsearch",
        tags: new[] { "search", "elasticsearch" })
    .AddKafka(
        new Confluent.Kafka.ProducerConfig { BootstrapServers = builder.Configuration["Kafka:Host"] ?? "localhost:9092" },
        name: "kafka",
        tags: new[] { "messaging", "ready" });

var app = builder.Build();

// Initialize Elasticsearch index
await app.Services.InitializeElasticsearchAsync();

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
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("search")
});
app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});

// Startup logging
app.Logger.LogInformation("SearchWorker starting up");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("PostgreSQL Connection: {Connection}",
    builder.Configuration.GetConnectionString("WriteServiceDb"));
app.Logger.LogInformation("Elasticsearch URL: {Url}",
    builder.Configuration.GetSection("Elasticsearch:Url").Value);
app.Logger.LogInformation("Elasticsearch Index: {Index}",
    builder.Configuration.GetSection("Elasticsearch:IndexName").Value);
app.Logger.LogInformation("Kafka Host: {Host}",
    builder.Configuration["Kafka:Host"]);

app.Run();
