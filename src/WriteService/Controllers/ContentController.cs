using Microsoft.AspNetCore.Mvc;
using WriteService.Infrastructure.Jobs;

namespace WriteService.Controllers;

/// <summary>
/// API endpoints for content operations
/// These endpoints are called by HangfireWorker jobs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ContentSyncJob _contentSyncJob;
    private readonly ILogger<ContentController> _logger;

    public ContentController(
        ContentSyncJob contentSyncJob,
        ILogger<ContentController> logger)
    {
        _contentSyncJob = contentSyncJob;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes content from all providers
    /// Called by HangfireWorker FrequentJob (every 5 minutes)
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncContent(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Content sync requested via API");

        try
        {
            await _contentSyncJob.ExecuteAsync(cancellationToken);

            return Ok(new
            {
                message = "Content synchronization completed successfully",
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content sync failed");
            return StatusCode(500, new
            {
                error = "Content synchronization failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "WriteService",
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
