using Microsoft.AspNetCore.Mvc;
using TimeService.Models;
using TimeService.Services.Orchestration;

namespace TimeService.Controllers;

/// <summary>
/// API Controller for TimeService score update operations
/// Provides endpoints for triggering score recalculations
/// </summary>
[ApiController]
[Route("api/time")]
public sealed class TimeServiceController : ControllerBase
{
    private readonly ITimeServiceOrchestrator _orchestrator;
    private readonly ILogger<TimeServiceController> _logger;

    public TimeServiceController(
        ITimeServiceOrchestrator orchestrator,
        ILogger<TimeServiceController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Updates scores for content crossing freshness thresholds today
    /// This is the endpoint called by HangfireWorker's DailyJob
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Score update result with statistics</returns>
    [HttpPost("update-daily")]
    [ProducesResponseType(typeof(ScoreUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDailyScores(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to update daily scores");

        try
        {
            var result = await _orchestrator.UpdateDailyScoresAsync(cancellationToken);

            _logger.LogInformation("Daily score update completed: {Summary}", result.GetSummary());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during daily score update");
            return StatusCode(500, new { error = "Internal server error during score update" });
        }
    }

    /// <summary>
    /// Forces recalculation of all content scores
    /// WARNING: This is expensive! Use only for manual correction or initial setup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Score update result with statistics</returns>
    [HttpPost("recalculate-all")]
    [ProducesResponseType(typeof(ScoreUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecalculateAllScores(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Received request to recalculate ALL scores (expensive operation)");

        try
        {
            var result = await _orchestrator.RecalculateAllScoresAsync(cancellationToken);

            _logger.LogInformation("Full score recalculation completed: {Summary}", result.GetSummary());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full score recalculation");
            return StatusCode(500, new { error = "Internal server error during score recalculation" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "TimeService",
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
