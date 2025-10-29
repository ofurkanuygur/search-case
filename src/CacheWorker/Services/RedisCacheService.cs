using CacheWorker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace CacheWorker.Services;

/// <summary>
/// Redis cache service implementation
/// Stores content with pre-calculated scores from database
/// NO score calculation happens here - following "Single Source of Truth" pattern
/// </summary>
public class RedisCacheService : ICacheService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ContentKeyPrefix = "searchcase:content:";
    private const string StatisticsKey = "searchcase:statistics:cache";
    private const string LastUpdatedKey = "searchcase:metadata:last_updated";

    public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        _defaultExpiration = TimeSpan.FromHours(configuration.GetValue<int>("Cache:ExpirationHours", 24));

        _redis = ConnectionMultiplexer.Connect(redisConnection);
        _database = _redis.GetDatabase();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _logger.LogInformation("Connected to Redis at {Connection}", redisConnection);
    }

    public async Task<CacheResult> UpdateCacheAsync(List<ContentEntity> contents, CancellationToken cancellationToken = default)
    {
        if (!contents.Any())
            return CacheResult.Success(0, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        var failedIds = new List<string>();
        var successCount = 0;

        try
        {
            var batch = _database.CreateBatch();
            var tasks = new List<Task<bool>>();

            foreach (var content in contents)
            {
                var key = $"{ContentKeyPrefix}{content.Id}";

                // Store content as Redis hash (matches SearchService expectations)
                // NOTE: Score is already included in the content from database
                var hashEntries = new HashEntry[]
                {
                    new HashEntry("id", content.Id),
                    new HashEntry("title", content.Title),
                    new HashEntry("type", content.ContentType.ToLower()),
                    new HashEntry("score", content.Score),
                    new HashEntry("publishedAt", content.PublishedAt.ToString("O")),
                    new HashEntry("categories", JsonSerializer.Serialize(content.Categories ?? new List<string>())),
                    new HashEntry("sourceProvider", content.SourceProvider),
                    new HashEntry("metrics", JsonSerializer.Serialize(content.Metadata ?? new Dictionary<string, object>()))
                };

                _ = batch.HashSetAsync(key, hashEntries);
                tasks.Add(batch.KeyExpireAsync(key, _defaultExpiration));

                // Also update sorted set for score-based queries
                // Using pre-calculated score from database
                // Matches SearchService schema: searchcase:leaderboard:score:*
                _ = batch.SortedSetAddAsync(
                    "searchcase:leaderboard:score:all",
                    content.Id,
                    content.Score);

                // Update sorted set by type (matches SearchService schema)
                _ = batch.SortedSetAddAsync(
                    $"searchcase:leaderboard:score:{content.ContentType.ToLower()}",
                    content.Id,
                    content.Score);
            }

            batch.Execute();
            var results = await Task.WhenAll(tasks);

            // Count successes and failures
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                {
                    successCount++;
                }
                else
                {
                    failedIds.Add(contents[i].Id);
                }
            }

            // Update last updated timestamp
            await _database.StringSetAsync(LastUpdatedKey, DateTimeOffset.UtcNow.ToString("O"));

            stopwatch.Stop();

            if (failedIds.Any())
            {
                _logger.LogWarning(
                    "Partial cache update: {Success}/{Total} succeeded, {Failed} failed",
                    successCount,
                    contents.Count,
                    failedIds.Count);

                return CacheResult.PartialSuccess(contents.Count, successCount, failedIds, stopwatch.Elapsed);
            }

            _logger.LogInformation(
                "Successfully cached {Count} contents in {Duration}ms (with pre-calculated scores)",
                successCount,
                stopwatch.ElapsedMilliseconds);

            return CacheResult.Success(successCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cache");
            stopwatch.Stop();
            return CacheResult.Failure(contents.Count, contents.Select(c => c.Id).ToList(), stopwatch.Elapsed);
        }
    }

    public async Task<ContentEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{ContentKeyPrefix}{id}";
            var hash = await _database.HashGetAllAsync(key);

            if (hash.Length == 0)
                return null;

            return DeserializeContentEntity(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content {Id} from cache", id);
            return null;
        }
    }

    public async Task<List<ContentEntity>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
        if (!ids.Any())
            return new List<ContentEntity>();

        try
        {
            var batch = _database.CreateBatch();
            var tasks = ids.Select(id => batch.HashGetAllAsync($"{ContentKeyPrefix}{id}")).ToArray();

            batch.Execute();
            await Task.WhenAll(tasks);

            var contents = new List<ContentEntity>();
            foreach (var task in tasks)
            {
                if (task.Result.Length > 0)
                {
                    var content = DeserializeContentEntity(task.Result);
                    if (content != null)
                        contents.Add(content);
                }
            }

            return contents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get multiple contents from cache");
            return new List<ContentEntity>();
        }
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{ContentKeyPrefix}{id}";
            var removed = await _database.KeyDeleteAsync(key);

            // Also remove from sorted sets
            await _database.SortedSetRemoveAsync("searchcase:leaderboard:score:all", id);

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove content {Id} from cache", id);
            return false;
        }
    }

    public async Task UpdateStatisticsAsync(string changeType, int count, CancellationToken cancellationToken = default)
    {
        try
        {
            var field = changeType.ToLower() switch
            {
                "created" => "total_created",
                "updated" => "total_updated",
                "mixed" => "total_mixed",
                _ => "total_changes"
            };

            await _database.HashIncrementAsync(StatisticsKey, field, count);
            await _database.HashSetAsync(StatisticsKey, "last_update", DateTimeOffset.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update statistics");
        }
    }

    public async Task<Dictionary<string, string>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _database.HashGetAllAsync(StatisticsKey);
            var result = new Dictionary<string, string>();

            foreach (var entry in stats)
            {
                result[entry.Name!] = entry.Value!;
            }

            // Add current cache size
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var keys = server.Keys(pattern: "searchcase:content:*").Count();
            result["cache_size"] = keys.ToString();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return new Dictionary<string, string>();
        }
    }

    public async Task<bool> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Best practice: Use pattern-based deletion instead of FLUSHDB (requires admin mode)
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var db = _redis.GetDatabase();

            // Find all content-related keys using pattern matching
            var keys = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: "searchcase:*"))
            {
                keys.Add(key);

                // Delete in batches of 100 to avoid blocking
                if (keys.Count >= 100)
                {
                    await db.KeyDeleteAsync(keys.ToArray());
                    keys.Clear();
                }
            }

            // Delete remaining keys
            if (keys.Count > 0)
            {
                await db.KeyDeleteAsync(keys.ToArray());
            }

            // Statistics and metadata keys are now included in searchcase:* pattern
            // No need for separate deletion

            _logger.LogWarning("All cache cleared using pattern-based deletion");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            return false;
        }
    }

    private ContentEntity? DeserializeContentEntity(HashEntry[] hash)
    {
        try
        {
            var dict = hash.ToDictionary(
                h => h.Name.ToString(),
                h => h.Value.ToString()
            );

            return new ContentEntity
            {
                Id = dict["id"],
                Title = dict["title"],
                ContentType = dict["type"],
                Score = double.Parse(dict["score"]),
                PublishedAt = DateTimeOffset.Parse(dict["publishedAt"]),
                Categories = dict.ContainsKey("categories")
                    ? JsonSerializer.Deserialize<List<string>>(dict["categories"])
                    : new List<string>(),
                SourceProvider = dict["sourceProvider"],
                Metadata = dict.ContainsKey("metrics")
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(dict["metrics"])
                    : new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize content entity from Redis hash");
            return null;
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}