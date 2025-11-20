using Serilog;
using MassTransit;
using EventBusService.Services;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EventBus Service");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/eventbus-.txt", rollingInterval: RollingInterval.Day));

    // Add Controllers
    builder.Services.AddControllers();

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "EventBus Service API",
            Version = "v1",
            Description = "Event publishing service for content change events using Apache Kafka"
        });
    });

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
            // Configure Kafka producer for ContentBatchUpdatedEvent
            rider.AddProducer<EventBusContracts.ContentBatchUpdatedEvent>(
                builder.Configuration["Kafka:Topic"] ?? "content-batch-updated");

            rider.UsingKafka((context, kafka) =>
            {
                // Configure Kafka connection
                var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
                kafka.Host(bootstrapServers);
            });
        });
    });

    // Register services
    builder.Services.AddScoped<IEventPublisher, EventPublisher>();

    // Health checks
    // Note: MassTransit provides built-in health checks for Kafka via bus health
    builder.Services.AddHealthChecks()
        .AddCheck("kafka-producer", () =>
        {
            // Basic health check - MassTransit handles Kafka connection health
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Kafka producer is configured");
        }, tags: new[] { "messaging", "ready" });

    var app = builder.Build();

    // Configure middleware
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventBus Service V1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseRouting();
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Always healthy if the app is running
    });

    // Root endpoint
    app.MapGet("/", () => new
    {
        service = "EventBus Service",
        status = "Running",
        endpoints = new[]
        {
            "/api/events/content-changed",
            "/api/events/content-changed/batch",
            "/health",
            "/swagger"
        }
    });

    Log.Information("EventBus Service started successfully");
    Log.Information("Kafka Bootstrap Servers: {BootstrapServers}", builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092");
    Log.Information("Kafka Topic: {Topic}", builder.Configuration["Kafka:Topic"] ?? "content-batch-updated");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EventBus Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}