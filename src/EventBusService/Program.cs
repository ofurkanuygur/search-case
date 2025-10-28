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
            Description = "Event publishing service for content change events using RabbitMQ"
        });
    });

    // Configure MassTransit with RabbitMQ
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            // Configure RabbitMQ connection
            var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitMqPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
            var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

            cfg.Host(rabbitMqHost, "/", h =>
            {
                h.Username(rabbitMqUsername);
                h.Password(rabbitMqPassword);
            });

            // Configure exchange and queues
            cfg.Publish<EventBusService.Events.ContentChangedEvent>(p =>
            {
                p.ExchangeType = "fanout"; // Fanout exchange for pub/sub pattern
            });

            // Configure retry policy
            cfg.UseMessageRetry(r => r.Exponential(5,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(5)));

            // Configure error handling
            cfg.ConfigureEndpoints(context);
        });
    });

    // Register services
    builder.Services.AddScoped<IEventPublisher, EventPublisher>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddRabbitMQ(
            rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMQ:Username"]}:" +
                                   $"{builder.Configuration["RabbitMQ:Password"]}@" +
                                   $"{builder.Configuration["RabbitMQ:Host"]}:" +
                                   $"{builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672)}/",
            name: "rabbitmq",
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
    Log.Information("RabbitMQ Host: {Host}", builder.Configuration["RabbitMQ:Host"] ?? "localhost");

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