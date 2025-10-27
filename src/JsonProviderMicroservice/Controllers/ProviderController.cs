using JsonProviderMicroservice.Services;
using Microsoft.AspNetCore.Mvc;
using SearchCase.Contracts.Responses;

namespace JsonProviderMicroservice.Controllers;

/// <summary>
/// Controller for provider data operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProviderController : ControllerBase
{
    private readonly IProviderService _providerService;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(
        IProviderService providerService,
        ILogger<ProviderController> logger)
    {
        _providerService = providerService ?? throw new ArgumentNullException(nameof(providerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets content data from the external provider in canonical format
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Canonical provider response with transformed content</returns>
    /// <response code="200">Returns the canonical content data successfully</response>
    /// <response code="500">Internal server error occurred</response>
    /// <response code="503">Service unavailable (circuit breaker open)</response>
    [HttpGet("data")]
    [ProducesResponseType(typeof(ProviderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ProviderResponse>> GetData(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GET /api/provider/data endpoint called");

            var result = await _providerService.GetDataAsync(cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("circuit"))
        {
            _logger.LogWarning(ex, "Circuit breaker is open");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Service temporarily unavailable due to circuit breaker" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching provider data");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while processing your request" });
        }
    }
}
