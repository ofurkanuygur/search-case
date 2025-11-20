using MassTransit;
using SearchWorker.Configuration;
using SearchWorker.Consumers;
using Serilog;

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

// Configure MassTransit with Apache Kafka
builder.Services.AddMassTransit(x =>
{
    // Use InMemory for command/response (not event streaming)
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    // Add Kafka Rider for event streaming
    x.AddRider(rider =>
    {
        // Register consumer in Rider context for Kafka
        rider.AddConsumer<ContentBatchUpdatedConsumer>();

        rider.UsingKafka((context, kafka) =>
        {
            // Configure Kafka connection
            var kafkaConfig = builder.Configuration.GetSection("Kafka");
            var bootstrapServers = kafkaConfig["BootstrapServers"] ?? "kafka:9092";
            var groupId = kafkaConfig["GroupId"] ?? "search-workers";
            var topic = kafkaConfig["Topic"] ?? "content-batch-updated";

            kafka.Host(bootstrapServers);

            // Configure Kafka topic endpoint
            kafka.TopicEndpoint<EventBusContracts.ContentBatchUpdatedEvent>(topic, groupId, e =>
            {
                // Configure consumer for this topic endpoint
                e.ConfigureConsumer<ContentBatchUpdatedConsumer>(context);

                // Kafka consumer settings
                e.AutoOffsetReset = Enum.Parse<Confluent.Kafka.AutoOffsetReset>(
                    kafkaConfig["AutoOffsetReset"] ?? "Earliest",
                    ignoreCase: true);

                // Manual commit for reliability
                e.CheckpointInterval = TimeSpan.FromSeconds(30);
                e.CheckpointMessageCount = 100;

                // Concurrency - process multiple messages in parallel
                e.ConcurrentMessageLimit = 4;
                e.ConcurrentDeliveryLimit = 4;

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
        tags: new[] { "search", "elasticsearch" });
    // RabbitMQ health check removed - MassTransit manages its own connection

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
app.Logger.LogInformation("Kafka Bootstrap Servers: {BootstrapServers}",
    builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092");
app.Logger.LogInformation("Kafka Consumer Group: {GroupId}",
    builder.Configuration["Kafka:GroupId"] ?? "search-workers");
app.Logger.LogInformation("Kafka Topic: {Topic}",
    builder.Configuration["Kafka:Topic"] ?? "content-batch-updated");

app.Run();
