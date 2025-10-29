using CacheWorker.Models;

namespace CacheWorker.Services;

/// <summary>
/// Repository for fetching content from database
/// Read-only operations - following "Single Source of Truth" pattern
/// </summary>
public interface IContentRepository
{
    /// <summary>
    /// Get contents by their IDs (with pre-calculated scores)
    /// </summary>
    Task<List<ContentEntity>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all content count for statistics
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get content count by type
    /// </summary>
    Task<Dictionary<string, int>> GetCountByTypeAsync(CancellationToken cancellationToken = default);
}