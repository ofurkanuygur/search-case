using WriteService.Domain.Entities;
using WriteService.Domain.Models;

namespace WriteService.Application.Interfaces;

/// <summary>
/// Repository interface for bulk operations (Interface Segregation Principle)
/// </summary>
public interface IBulkOperationRepository
{
    /// <summary>
    /// Performs bulk upsert with batching for better performance
    /// </summary>
    /// <param name="contents">Contents to upsert</param>
    /// <param name="batchSize">Size of each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResult> BulkUpsertWithBatchingAsync(
        IEnumerable<ContentEntity> contents,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk insert for new content only
    /// </summary>
    Task<BulkOperationResult> BulkInsertAsync(
        IEnumerable<ContentEntity> contents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk update for existing content only
    /// </summary>
    Task<BulkOperationResult> BulkUpdateAsync(
        IEnumerable<ContentEntity> contents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk delete for removed content
    /// </summary>
    Task<BulkOperationResult> BulkDeleteAsync(
        IEnumerable<string> contentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only scores for existing content (for FreshnessScoreUpdateJob)
    /// Does not check content_hash, always updates score and updated_at
    /// </summary>
    Task<BulkOperationResult> BulkUpdateScoresAsync(
        IEnumerable<ContentEntity> contents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves change logs for audit trail
    /// </summary>
    Task SaveChangeLogsAsync(
        IEnumerable<ContentChangeLog> changeLogs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves sync batch information
    /// </summary>
    Task SaveSyncBatchAsync(
        SyncBatch syncBatch,
        CancellationToken cancellationToken = default);
}