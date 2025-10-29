using SearchCase.Search.Contracts.Enums;
using SearchCase.Search.Contracts.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace SearchService.Clients.Redis;

/// <summary>
/// Redis search client implementation using sorted sets and hashes
/// </summary>
public sealed class RedisSearchClient : IRedisSearchClient
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSearchClient> _logger;

    private IDatabase Db => _redis.GetDatabase();

    public RedisSearchClient(
        IConnectionMultiplexer redis,
        ILogger<RedisSearchClient> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ContentDto>> GetTopByScoreAsync(
        ContentType? type,
        int skip,
        int take,
        double? minScore = null,
        double? maxScore = null,
        CancellationToken cancellationToken = default)
    {
        var key = RedisKeySchema.ScoreLeaderboard(type);

        // ZREVRANGE: Get members from sorted set in descending order with score range (O(log(N)+M))
        var members = await Db.SortedSetRangeByScoreAsync(
            key: key,
            start: minScore ?? double.NegativeInfinity,
            stop: maxScore ?? double.PositiveInfinity,
            exclude: Exclude.None,
            order: Order.Descending,
            skip: skip,
            take: take
        );

        if (members.Length == 0)
        {
            _logger.LogDebug("No members found in {Key}", key);
            return Array.Empty<ContentDto>();
        }

        _logger.LogDebug(
            "Found {Count} members in {Key} (skip={Skip}, take={Take})",
            members.Length, key, skip, take);

        // Batch fetch content details using pipeline
        var batch = Db.CreateBatch();
        var tasks = members
            .Select(m => batch.HashGetAllAsync(RedisKeySchema.ContentHash(m.ToString())))
            .ToArray();

        batch.Execute();
        await Task.WhenAll(tasks);

        // Deserialize results
        var contents = tasks
            .Select(t => t.Result)
            .Where(h => h.Length > 0)
            .Select(DeserializeContent)
            .Where(c => c != null)
            .ToList();

        return contents!;
    }

    public async Task<ContentDto?> GetByIdAsync(
        string contentId,
        CancellationToken cancellationToken = default)
    {
        var key = RedisKeySchema.ContentHash(contentId);
        var hash = await Db.HashGetAllAsync(key);

        if (hash.Length == 0)
        {
            _logger.LogDebug("Content {ContentId} not found in Redis", contentId);
            return null;
        }

        return DeserializeContent(hash);
    }

    public async Task<int> GetTotalCountAsync(
        ContentType? type,
        CancellationToken cancellationToken = default)
    {
        var key = RedisKeySchema.ScoreLeaderboard(type);
        var count = await Db.SortedSetLengthAsync(key);
        return (int)count;
    }

    public async Task<SearchResult?> GetCachedQueryAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = RedisKeySchema.QueryCache(request);
        var json = await Db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
        {
            _logger.LogDebug("Query cache miss for {Key}", key);
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<SearchResult>(json!);
            _logger.LogDebug("Query cache hit for {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached query result");
            return null;
        }
    }

    public async Task SetCachedQueryAsync(
        SearchRequest request,
        SearchResult result,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var key = RedisKeySchema.QueryCache(request);
        var json = JsonSerializer.Serialize(result);

        await Db.StringSetAsync(key, json, expiration);

        _logger.LogDebug("Cached query result with key {Key} (TTL: {TTL}s)", key, expiration.TotalSeconds);
    }

    private ContentDto? DeserializeContent(HashEntry[] hash)
    {
        try
        {
            var dict = hash.ToDictionary(
                h => h.Name.ToString(),
                h => h.Value.ToString()
            );

            return new ContentDto
            {
                Id = dict["id"],
                Title = dict["title"],
                Type = Enum.Parse<ContentType>(dict["type"], ignoreCase: true),
                Score = double.Parse(dict["score"]),
                PublishedAt = DateTimeOffset.Parse(dict["publishedAt"]),
                Categories = JsonSerializer.Deserialize<List<string>>(dict["categories"]) ?? new List<string>(),
                SourceProvider = dict["sourceProvider"],
                Metrics = JsonSerializer.Deserialize<Dictionary<string, object>>(dict["metrics"])
                    ?? new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize content from Redis hash");
            return null;
        }
    }
}
