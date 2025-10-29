using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SearchCase.Search.Contracts.Models;
using SearchService.Orchestration;

namespace SearchService.Controllers;

/// <summary>
/// Search API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ISearchOrchestrator _searchOrchestrator;
    private readonly IValidator<SearchRequest> _validator;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchOrchestrator searchOrchestrator,
        IValidator<SearchRequest> validator,
        ILogger<SearchController> logger)
    {
        _searchOrchestrator = searchOrchestrator ?? throw new ArgumentNullException(nameof(searchOrchestrator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search content with keyword, filters, and pagination
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with pagination and metadata</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SearchResult>> Search(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Search request received: keyword={Keyword}, type={Type}, sort={Sort}, page={Page}, pageSize={PageSize}",
            request.Keyword, request.Type, request.Sort, request.Page, request.PageSize);

        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage })
                .ToList();

            _logger.LogWarning("Invalid search request: {Errors}", errors);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid search request",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            // Execute search with automatic strategy selection
            var result = await _searchOrchestrator.SearchAsync(request, cancellationToken);

            _logger.LogInformation(
                "Search completed: strategy={Strategy}, source={Source}, total={Total}, returned={Returned}, latency={Latency}ms",
                result.Metadata.Strategy,
                result.Metadata.DataSource,
                result.Pagination.TotalItems,
                result.Items.Count,
                result.Metadata.LatencyMs);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Search failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "SearchService",
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
