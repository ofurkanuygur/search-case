using CacheWorker.Models;

namespace CacheWorker.Services;

/// <summary>
/// Service for managing Redis cache operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Update cache with content entities (already includes pre-calculated scores)
    /// </summary>
    Task<CacheResult> UpdateCacheAsync(List<ContentEntity> contents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cached content by ID
    /// </summary>
    Task<ContentEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple cached contents by IDs
    /// </summary>
    Task<List<ContentEntity>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove content from cache
    /// </summary>
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update cache statistics
    /// </summary>
    Task UpdateStatisticsAsync(string changeType, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<Dictionary<string, string>> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task<bool> ClearAllAsync(CancellationToken cancellationToken = default);
}