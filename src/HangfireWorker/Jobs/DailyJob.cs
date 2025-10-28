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
        Logger.LogInformation("Executing DailyJob - triggering DailyJobService");

        // Prepare payload with daily job metadata
        var payload = new
        {
            JobName = "DailyJob",
            TriggeredAt = DateTime.UtcNow,
            ExecutionId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Message = "Daily scheduled execution from Hangfire"
        };

        // Trigger DailyJobService
        var response = await _microserviceClient.TriggerAsync(
            serviceName: "DailyJobService",
            endpoint: "/api/process",
            payload: payload,
            cancellationToken: cancellationToken);

        Logger.LogInformation(
            "DailyJob completed. DailyJobService response: {Response}",
            response);
    }

    protected override string GetJobDisplayName() => "Daily Job (Once per Day)";
}
