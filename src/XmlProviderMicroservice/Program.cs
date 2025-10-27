using XmlProviderMicroservice.Configuration;
using XmlProviderMicroservice.Extensions;
using Serilog;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting XML Provider Microservice");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add controllers with JSON options
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Add ISO 8601 duration converter
            options.JsonSerializerOptions.Converters.Add(new SearchCase.Contracts.Converters.Iso8601DurationConverter());

            // Serialize enums as strings (camelCase)
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        });

    // Add API Explorer for Swagger
    builder.Services.AddEndpointsApiExplorer();

    // Add Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "XML Provider Microservice API",
            Version = "v1",
            Description = "Microservice for fetching and serving data from external XML provider in canonical format",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "SearchCase Team"
            }
        });

        // Configure polymorphic serialization for discriminated unions
        options.UseOneOfForPolymorphism();
        options.SelectDiscriminatorNameUsing(baseType => "type");

        // Include XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Add External API Client with resilience patterns
    builder.Services.AddExternalApiClient(builder.Configuration);

    // Add Application Services
    builder.Services.AddApplicationServices();

    // Add Health Checks
    var externalApiSettings = builder.Configuration
        .GetSection(ExternalApiSettings.SectionName)
        .Get<ExternalApiSettings>() ?? new ExternalApiSettings();

    builder.Services.AddHealthChecks()
        .AddUrlGroup(
            new Uri(externalApiSettings.BaseUrl),
            name: "external-api",
            tags: new[] { "external", "ready" },
            timeout: TimeSpan.FromSeconds(5),
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    // Enable Swagger in all environments for this microservice
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "XML Provider API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "XML Provider API";
    });

    // Health check endpoints
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Always healthy if running
    });

    app.MapControllers();

    Log.Information("XML Provider Microservice configured successfully");
    Log.Information("Swagger UI available at: /swagger");
    Log.Information("API endpoint: GET /api/provider/data");
    Log.Information("Health checks: /health, /health/ready, /health/live");

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
