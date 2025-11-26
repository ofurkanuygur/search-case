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
            Description = "Event publishing service for content change events using Kafka"
        });
    });

    // Configure MassTransit with Kafka
    builder.Services.AddMassTransit(x =>
    {
        x.UsingInMemory((context, cfg) => 
        {
            // No endpoints needed for producer-only service
        });

        x.AddRider(rider =>
        {
            rider.AddProducer<EventBusService.Events.ContentChangedEvent>("content-events");
            rider.AddProducer<EventBusContracts.ContentBatchUpdatedEvent>("content-batch-events");

            rider.UsingKafka((context, k) =>
            {
                var kafkaHost = builder.Configuration["Kafka:Host"] ?? "localhost:9094";
                k.Host(kafkaHost);
            });
        });
    });

    // Register services
    builder.Services.AddScoped<IEventPublisher, EventPublisher>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddKafka(
            new Confluent.Kafka.ProducerConfig { BootstrapServers = builder.Configuration["Kafka:Host"] ?? "localhost:9094" },
            name: "kafka",
            tags: new[] { "messaging", "ready" });

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
    Log.Information("Kafka Host: {Host}", builder.Configuration["Kafka:Host"] ?? "localhost:9094");

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