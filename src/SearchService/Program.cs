using Serilog;
using SearchService.Extensions;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Search Service");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/searchservice-.txt", rollingInterval: RollingInterval.Day));

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "SearchService API",
            Version = "v1",
            Description = "Hybrid search API with Redis and Elasticsearch backends"
        });
    });

    // Add search services (Redis, Elasticsearch, Strategies, Orchestrator)
    builder.Services.AddSearchServices(builder.Configuration);

    // Health checks
    builder.Services.AddHealthChecks();

    // CORS (for Dashboard)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowDashboard", policy =>
        {
            policy.WithOrigins("http://localhost:9000", "http://localhost:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure middleware
    app.UseSerilogRequestLogging();

    // Swagger (always enabled for development)
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SearchService API v1");
        options.RoutePrefix = "swagger";
    });

    app.UseCors("AllowDashboard");

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Search Service started successfully");
    Log.Information("Swagger UI available at: /swagger");
    Log.Information("API available at: /api/search");
    Log.Information("Health check available at: /health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Search Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
