using SearchCase.Search.Contracts.Enums;
using SearchCase.Search.Contracts.Models;
using SearchService.Clients.Redis;
using System.Diagnostics;

namespace SearchService.Strategies;

/// <summary>
/// Redis search strategy for simple queries (no keyword, sorted by score)
/// Fast O(log(N)+M) performance using sorted sets
/// </summary>
public sealed class RedisSearchStrategy : ISearchStrategy
{
    private readonly IRedisSearchClient _redisClient;
    private readonly ILogger<RedisSearchStrategy> _logger;

    public string Name => "Redis";
    public int Priority => 3; // Highest priority (fastest)

    public RedisSearchStrategy(
        IRedisSearchClient redisClient,
        ILogger<RedisSearchStrategy> logger)
    {
        _redisClient = redisClient ?? throw new ArgumentNullException(nameof(redisClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(SearchRequest request)
    {
        // Can handle: no keyword, only type filter and score sorting
        var canHandle = string.IsNullOrWhiteSpace(request.Keyword) &&
                       request.Sort == SortBy.Score;

        _logger.LogDebug(
            "Redis strategy can handle: {CanHandle} (keyword={Keyword}, sort={Sort})",
            canHandle, request.Keyword, request.Sort);

        return canHandle;
    }

    public async Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Executing Redis search: type={Type}, page={Page}, pageSize={PageSize}",
            request.Type, request.Page, request.PageSize);

        try
        {
            var skip = (request.Page - 1) * request.PageSize;

            // Get content from Redis sorted set with score range filter
            var items = await _redisClient.GetTopByScoreAsync(
                request.Type,
                skip,
                request.PageSize,
                request.MinScore,
                request.MaxScore,
                cancellationToken
            );

            // Get total count
            var totalCount = await _redisClient.GetTotalCountAsync(
                request.Type,
                cancellationToken
            );

            stopwatch.Stop();

            _logger.LogInformation(
                "Redis search completed: {Total} results, {Returned} returned, {Latency}ms",
                totalCount, items.Count, stopwatch.ElapsedMilliseconds);

            return new SearchResult
            {
                Items = items,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = totalCount
                },
                Metadata = new SearchMetadata
                {
                    Strategy = Name,
                    DataSource = "Redis",
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    FromCache = false,
                    Timestamp = DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis search failed");
            throw;
        }
    }
}
