using SearchCase.Search.Contracts.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SearchCase.Search.Contracts.Models;

namespace SearchService.Clients.Redis;

/// <summary>
/// Redis key naming schema (namespace pattern)
/// </summary>
public static class RedisKeySchema
{
    private const string Namespace = "searchcase";

    /// <summary>
    /// Sorted set for score-based ranking
    /// Key: searchcase:leaderboard:score:{type}
    /// </summary>
    public static string ScoreLeaderboard(ContentType? type = null)
        => type.HasValue
            ? $"{Namespace}:leaderboard:score:{type.Value.ToString().ToLowerInvariant()}"
            : $"{Namespace}:leaderboard:score:all";

    /// <summary>
    /// Sorted set for date-based ranking
    /// Key: searchcase:index:date:{type}
    /// </summary>
    public static string DateIndex(ContentType? type = null)
        => type.HasValue
            ? $"{Namespace}:index:date:{type.Value.ToString().ToLowerInvariant()}"
            : $"{Namespace}:index:date:all";

    /// <summary>
    /// Hash for content details
    /// Key: searchcase:content:{contentId}
    /// </summary>
    public static string ContentHash(string contentId)
        => $"{Namespace}:content:{contentId}";

    /// <summary>
    /// Set for category grouping
    /// Key: searchcase:category:{category}
    /// </summary>
    public static string CategorySet(string category)
        => $"{Namespace}:category:{category.ToLowerInvariant()}";

    /// <summary>
    /// String for query result cache (Hybrid strategy)
    /// Key: searchcase:cache:query:{hash}
    /// TTL: 5 minutes
    /// </summary>
    public static string QueryCache(SearchRequest request)
    {
        var hash = ComputeHash(request);
        return $"{Namespace}:cache:query:{hash}";
    }

    private static string ComputeHash(SearchRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // First 16 characters
    }
}
