using SearchCase.HangfireWorker.Services;

namespace SearchCase.HangfireWorker.Jobs;

/// <summary>
/// Job that runs daily
/// Triggers Microservice B
/// </summary>
public sealed class DailyJob : BaseJob
{
    private readonly IMicroserviceClient _microserviceClient;

    public DailyJob(
        IMicroserviceClient microserviceClient,
        ILogger<DailyJob> logger) : base(logger)
    {
        _microserviceClient = microserviceClient ?? throw new ArgumentNullException(nameof(microserviceClient));
    }

    protected override async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing DailyJob - triggering TimeService for daily score updates");

        // Trigger TimeService to update scores for content crossing freshness thresholds
        // TimeService will:
        // 1. Fetch content published exactly 7, 30, or 90 days ago (threshold optimization)
        // 2. Recalculate scores using freshness + engagement formulas
        // 3. Bulk update database
        // 4. Publish ContentBatchUpdatedEvent to EventBus → CacheWorker + SearchWorker
        var response = await _microserviceClient.TriggerAsync(
            serviceName: "TimeService",
            endpoint: "/api/time/update-daily",
            payload: null, // No payload needed - TimeService determines threshold dates internally
            cancellationToken: cancellationToken);

        Logger.LogInformation(
            "DailyJob completed. TimeService daily score update response: {Response}",
            response);
    }

    protected override string GetJobDisplayName() => "Daily Job (Once per Day)";
}
