using TimeService.Domain.Entities;

namespace TimeService.Infrastructure.Repository;

/// <summary>
/// Repository interface for score-related database operations
/// SOLID: Interface Segregation - Only score update operations
/// SOLID: Dependency Inversion - Depend on abstraction
/// </summary>
public interface IScoreRepository
{
    /// <summary>
    /// Gets all content from database
    /// Used to find content crossing freshness thresholds
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All content entities</returns>
    Task<List<ContentEntity>> GetAllContentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets content published on specific dates (for threshold optimization)
    /// </summary>
    /// <param name="publishDates">List of publish dates to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content published on specified dates</returns>
    Task<List<ContentEntity>> GetContentByPublishDatesAsync(
        List<DateTimeOffset> publishDates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates scores for multiple content items
    /// Uses raw SQL for performance optimization
    /// </summary>
    /// <param name="contentUpdates">List of content entities with updated scores</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records updated</returns>
    Task<int> BulkUpdateScoresAsync(
        List<ContentEntity> contentUpdates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a single content's score
    /// </summary>
    /// <param name="content">Content entity with updated score</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update successful</returns>
    Task<bool> UpdateScoreAsync(
        ContentEntity content,
        CancellationToken cancellationToken = default);
}
