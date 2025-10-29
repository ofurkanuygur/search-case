using Hangfire;
using Microsoft.AspNetCore.Mvc;
using WriteService.Infrastructure.Jobs;

namespace WriteService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public TestController(
        ILogger<TestController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Triggers TimeService daily score update (delegates to TimeService)
    /// This is what HangfireWorker DailyJob does
    /// </summary>
    [HttpPost("trigger-time-service")]
    public async Task<IActionResult> TriggerTimeService()
    {
        _logger.LogInformation("Manual trigger requested for TimeService /api/time/update-daily");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://time-service:8080");
            client.Timeout = TimeSpan.FromMinutes(5);

            var response = await client.PostAsync("/api/time/update-daily", null);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("TimeService responded successfully: {Content}", content);
                return Ok(new
                {
                    message = "TimeService update-daily triggered successfully",
                    timeServiceResponse = content
                });
            }
            else
            {
                _logger.LogError("TimeService returned error: {StatusCode} - {Content}",
                    response.StatusCode, content);
                return StatusCode((int)response.StatusCode, new
                {
                    error = "TimeService returned error",
                    details = content
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call TimeService");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Internal testing: Trigger FreshnessScoreUpdateJob directly in WriteService
    /// (Normally this is handled by TimeService via HangfireWorker)
    /// </summary>
    [HttpPost("trigger-freshness-job-local")]
    public IActionResult TriggerFreshnessJobLocal()
    {
        _logger.LogInformation("Manual trigger requested for FreshnessScoreUpdateJob (local)");

        var jobId = BackgroundJob.Enqueue<FreshnessScoreUpdateJob>(
            job => job.ExecuteAsync(default));

        _logger.LogInformation("FreshnessScoreUpdateJob enqueued with ID: {JobId}", jobId);

        return Ok(new
        {
            message = "FreshnessScoreUpdateJob triggered locally (WriteService)",
            jobId = jobId,
            dashboardUrl = $"/hangfire/jobs/details/{jobId}",
            note = "Normally TimeService handles this via HangfireWorker DailyJob"
        });
    }

    [HttpPost("trigger-content-sync-job")]
    public IActionResult TriggerContentSyncJob()
    {
        _logger.LogInformation("Manual trigger requested for ContentSyncJob");

        var jobId = BackgroundJob.Enqueue<ContentSyncJob>(
            job => job.ExecuteAsync(default));

        _logger.LogInformation("ContentSyncJob enqueued with ID: {JobId}", jobId);

        return Ok(new
        {
            message = "ContentSyncJob triggered successfully",
            jobId = jobId,
            dashboardUrl = $"/hangfire/jobs/details/{jobId}"
        });
    }
}
