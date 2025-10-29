using CacheWorker.Services;
using Microsoft.AspNetCore.Mvc;

namespace CacheWorker.Controllers;

/// <summary>
/// API controller for cache operations and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IContentRepository _contentRepository;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        IContentRepository contentRepository,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get cached content by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var content = await _cacheService.GetByIdAsync(id);
        if (content == null)
        {
            return NotFound(new { message = $"Content {id} not found in cache" });
        }

        return Ok(content);
    }

    /// <summary>
    /// Get multiple cached contents by IDs
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> GetByIds([FromBody] List<string> ids)
    {
        if (!ids.Any())
        {
            return BadRequest(new { message = "No IDs provided" });
        }

        var contents = await _cacheService.GetByIdsAsync(ids);
        return Ok(new
        {
            requested = ids.Count,
            found = contents.Count,
            contents
        });
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = await _cacheService.GetStatisticsAsync();
        var dbStats = await _contentRepository.GetCountByTypeAsync();

        return Ok(new
        {
            cache = stats,
            database = new
            {
                total = await _contentRepository.GetTotalCountAsync(),
                byType = dbStats
            }
        });
    }

    /// <summary>
    /// Manually refresh cache for specific IDs (fetches from DB with pre-calculated scores)
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshCache([FromBody] List<string> ids)
    {
        if (!ids.Any())
        {
            return BadRequest(new { message = "No IDs provided" });
        }

        try
        {
            // Fetch from database (with pre-calculated scores)
            var contents = await _contentRepository.GetByIdsAsync(ids);

            if (!contents.Any())
            {
                return NotFound(new { message = "No contents found in database" });
            }

            // Update cache with fetched content
            var result = await _cacheService.UpdateCacheAsync(contents);

            return Ok(new
            {
                message = "Cache refresh completed",
                result = new
                {
                    requested = ids.Count,
                    found = contents.Count,
                    cached = result.SuccessCount,
                    failed = result.FailedCount,
                    duration = result.Duration.TotalMilliseconds
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh cache");
            return StatusCode(500, new { message = "Failed to refresh cache" });
        }
    }

    /// <summary>
    /// Clear specific content from cache
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromCache(string id)
    {
        var removed = await _cacheService.RemoveAsync(id);
        if (!removed)
        {
            return NotFound(new { message = $"Content {id} not found in cache" });
        }

        return Ok(new { message = $"Content {id} removed from cache" });
    }

    /// <summary>
    /// Clear all cache (use with caution)
    /// </summary>
    [HttpDelete("all")]
    public async Task<IActionResult> ClearAllCache()
    {
        var cleared = await _cacheService.ClearAllAsync();
        if (!cleared)
        {
            return StatusCode(500, new { message = "Failed to clear cache" });
        }

        return Ok(new { message = "All cache cleared successfully" });
    }
}