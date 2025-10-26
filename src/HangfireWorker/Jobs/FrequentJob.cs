using SearchCase.HangfireWorker.Services;

namespace SearchCase.HangfireWorker.Jobs;

/// <summary>
/// Job that runs frequently (every 5 minutes)
/// Triggers Microservice A
/// </summary>
public sealed class FrequentJob : BaseJob
{
    private readonly IMicroserviceClient _microserviceClient;

    public FrequentJob(
        IMicroserviceClient microserviceClient,
        ILogger<FrequentJob> logger) : base(logger)
    {
        _microserviceClient = microserviceClient ?? throw new ArgumentNullException(nameof(microserviceClient));
    }

    protected override async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing FrequentJob - triggering Microservice A");

        // Prepare payload with job metadata
        var payload = new
        {
            JobName = "FrequentJob",
            TriggeredAt = DateTime.UtcNow,
            ExecutionId = Guid.NewGuid(),
            Message = "Scheduled execution from Hangfire"
        };

        // Trigger Microservice A
        var response = await _microserviceClient.TriggerAsync(
            serviceName: "ServiceA",
            endpoint: "/api/process",
            payload: payload,
            cancellationToken: cancellationToken);

        Logger.LogInformation(
            "FrequentJob completed. Microservice A response: {Response}",
            response);
    }

    protected override string GetJobDisplayName() => "Frequent Job (Every 5 Minutes)";
}
