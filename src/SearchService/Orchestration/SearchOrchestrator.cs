using SearchCase.Search.Contracts.Models;
using SearchService.Strategies;
using System.Diagnostics;

namespace SearchService.Orchestration;

/// <summary>
/// Search orchestrator implementing Chain of Responsibility pattern
/// Selects and executes the best strategy for each query
/// </summary>
public sealed class SearchOrchestrator : ISearchOrchestrator
{
    private readonly IEnumerable<ISearchStrategy> _strategies;
    private readonly ILogger<SearchOrchestrator> _logger;

    public SearchOrchestrator(
        IEnumerable<ISearchStrategy> strategies,
        ILogger<SearchOrchestrator> logger)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!_strategies.Any())
        {
            throw new InvalidOperationException("No search strategies registered");
        }
    }

    public async Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Select best strategy (Chain of Responsibility pattern)
        var strategy = SelectStrategy(request);

        _logger.LogInformation(
            "Selected {Strategy} for query: keyword={Keyword}, type={Type}, sort={Sort}, page={Page}",
            strategy.Name, request.Keyword, request.Type, request.Sort, request.Page);

        try
        {
            var result = await strategy.SearchAsync(request, cancellationToken);

            stopwatch.Stop();

            // Enrich metadata with total orchestration time
            result = result with
            {
                Metadata = result.Metadata with
                {
                    LatencyMs = stopwatch.ElapsedMilliseconds
                }
            };

            _logger.LogInformation(
                "Search completed with {Strategy}: {Total} results, {Latency}ms",
                strategy.Name, result.Pagination.TotalItems, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Search failed with {Strategy}, attempting fallback",
                strategy.Name);

            // Fallback to next available strategy
            return await FallbackSearchAsync(request, strategy, cancellationToken);
        }
    }

    private ISearchStrategy SelectStrategy(SearchRequest request)
    {
        // Chain of Responsibility: try strategies by priority
        var strategy = _strategies
            .Where(s => s.CanHandle(request))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

        if (strategy == null)
        {
            throw new InvalidOperationException(
                $"No strategy found for request: keyword={request.Keyword}, sort={request.Sort}");
        }

        return strategy;
    }

    private async Task<SearchResult> FallbackSearchAsync(
        SearchRequest request,
        ISearchStrategy failedStrategy,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Attempting fallback from {Strategy}", failedStrategy.Name);

        // Try next available strategy
        var fallback = _strategies
            .Where(s => s != failedStrategy && s.CanHandle(request))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

        if (fallback == null)
        {
            _logger.LogError("No fallback strategy available, returning empty result");

            // No fallback available - return empty result
            return new SearchResult
            {
                Items = Array.Empty<ContentDto>(),
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 0
                },
                Metadata = new SearchMetadata
                {
                    Strategy = "Fallback",
                    DataSource = "None",
                    LatencyMs = 0,
                    FromCache = false,
                    Timestamp = DateTimeOffset.UtcNow
                }
            };
        }

        _logger.LogInformation("Using fallback strategy: {Fallback}", fallback.Name);

        return await fallback.SearchAsync(request, cancellationToken);
    }
}
