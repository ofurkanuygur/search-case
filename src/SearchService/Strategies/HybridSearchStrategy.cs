using SearchCase.Search.Contracts.Models;
using SearchService.Clients.Elasticsearch;
using SearchService.Clients.Redis;
using System.Diagnostics;

namespace SearchService.Strategies;

/// <summary>
/// Hybrid search strategy that caches popular queries in Redis
/// First checks Redis cache, falls back to Elasticsearch, then caches result
/// </summary>
public sealed class HybridSearchStrategy : ISearchStrategy
{
    private readonly IRedisSearchClient _redisClient;
    private readonly IElasticsearchSearchClient _elasticsearchClient;
    private readonly ILogger<HybridSearchStrategy> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public string Name => "Hybrid";
    public int Priority => 2; // Medium priority

    public HybridSearchStrategy(
        IRedisSearchClient redisClient,
        IElasticsearchSearchClient elasticsearchClient,
        ILogger<HybridSearchStrategy> logger)
    {
        _redisClient = redisClient ?? throw new ArgumentNullException(nameof(redisClient));
        _elasticsearchClient = elasticsearchClient ?? throw new ArgumentNullException(nameof(elasticsearchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(SearchRequest request)
    {
        // Handle keyword queries that might be popular
        // (In production, you'd track query frequency)
        var canHandle = !string.IsNullOrWhiteSpace(request.Keyword);

        _logger.LogDebug(
            "Hybrid strategy can handle: {CanHandle} (keyword={Keyword})",
            canHandle, request.Keyword);

        return canHandle;
    }

    public async Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Executing Hybrid search: keyword={Keyword}, checking cache first",
            request.Keyword);

        try
        {
            // Try Redis cache first
            var cachedResult = await _redisClient.GetCachedQueryAsync(request, cancellationToken);

            if (cachedResult != null)
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "Cache HIT for keyword={Keyword}, {Latency}ms",
                    request.Keyword, stopwatch.ElapsedMilliseconds);

                // Update metadata
                return cachedResult with
                {
                    Metadata = cachedResult.Metadata with
                    {
                        Strategy = Name,
                        DataSource = "Redis (Cached)",
                        LatencyMs = stopwatch.ElapsedMilliseconds,
                        FromCache = true,
                        Timestamp = DateTimeOffset.UtcNow
                    }
                };
            }

            _logger.LogInformation(
                "Cache MISS for keyword={Keyword}, querying Elasticsearch",
                request.Keyword);

            // Cache miss - query Elasticsearch
            var result = await _elasticsearchClient.SearchAsync(request, cancellationToken);

            stopwatch.Stop();

            // Cache the result for future requests
            await _redisClient.SetCachedQueryAsync(
                request,
                result,
                CacheTtl,
                cancellationToken
            );

            _logger.LogInformation(
                "Cached query result for keyword={Keyword} (TTL: {TTL}s), total {Latency}ms",
                request.Keyword, CacheTtl.TotalSeconds, stopwatch.ElapsedMilliseconds);

            // Update metadata
            return result with
            {
                Metadata = result.Metadata with
                {
                    Strategy = Name,
                    DataSource = "Elasticsearch → Redis (Cached)",
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    FromCache = false
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hybrid search failed");
            throw;
        }
    }
}
