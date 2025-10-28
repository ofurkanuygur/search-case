using Microsoft.AspNetCore.Mvc;
using SearchWorker.Services;

namespace SearchWorker.Controllers;

/// <summary>
/// Controller for search operations
/// Provides endpoints for searching and managing Elasticsearch index
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IElasticsearchService elasticsearchService,
        ILogger<SearchController> logger)
    {
        _elasticsearchService = elasticsearchService ?? throw new ArgumentNullException(nameof(elasticsearchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search content by query
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        var result = await _elasticsearchService.SearchAsync(query, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken = default)
    {
        var document = await _elasticsearchService.GetByIdAsync(id, cancellationToken);

        if (document == null)
        {
            return NotFound(new { error = $"Document with ID '{id}' not found" });
        }

        return Ok(document);
    }

    /// <summary>
    /// Get index statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var stats = await _elasticsearchService.GetIndexStatsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Check if index exists
    /// </summary>
    [HttpGet("index/exists")]
    public async Task<IActionResult> IndexExists(CancellationToken cancellationToken = default)
    {
        var exists = await _elasticsearchService.IndexExistsAsync(cancellationToken);
        return Ok(new { exists });
    }

    /// <summary>
    /// Create index (if not exists)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> CreateIndex(CancellationToken cancellationToken = default)
    {
        var exists = await _elasticsearchService.IndexExistsAsync(cancellationToken);
        if (exists)
        {
            return Conflict(new { error = "Index already exists" });
        }

        var created = await _elasticsearchService.CreateIndexAsync(cancellationToken);
        if (created)
        {
            return Ok(new { message = "Index created successfully" });
        }

        return StatusCode(500, new { error = "Failed to create index" });
    }

    /// <summary>
    /// Delete index (use with caution!)
    /// </summary>
    [HttpDelete("index")]
    public async Task<IActionResult> DeleteIndex(CancellationToken cancellationToken = default)
    {
        var deleted = await _elasticsearchService.DeleteIndexAsync(cancellationToken);
        if (deleted)
        {
            return Ok(new { message = "Index deleted successfully" });
        }

        return StatusCode(500, new { error = "Failed to delete index" });
    }
}
