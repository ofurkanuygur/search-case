using Microsoft.Extensions.Logging;
using SearchCase.Contracts.Models;
using WriteService.Application.Interfaces;
using WriteService.Data.Repositories;
using WriteService.Domain.Entities;
using WriteService.Domain.Enums;
using WriteService.Domain.Models;

namespace WriteService.Application.Services;

/// <summary>
/// Orchestrates content synchronization with change detection and bulk operations
/// (Single Responsibility: Coordination)
/// </summary>
public sealed class ContentSyncOrchestrator
{
    private readonly IChangeDetectionStrategy _changeDetectionStrategy;
    private readonly IBulkOperationRepository _bulkRepository;
    private readonly IContentRepository _contentRepository;
    private readonly ILogger<ContentSyncOrchestrator> _logger;

    public ContentSyncOrchestrator(
        IChangeDetectionStrategy changeDetectionStrategy,
        IBulkOperationRepository bulkRepository,
        IContentRepository contentRepository,
        ILogger<ContentSyncOrchestrator> logger)
    {
        _changeDetectionStrategy = changeDetectionStrategy ?? throw new ArgumentNullException(nameof(changeDetectionStrategy));
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Synchronizes content with change detection and bulk operations
    /// Score is calculated ONLY for changed items (optimization)
    /// </summary>
    public async Task<SyncResult> SynchronizeContentAsync(
        IEnumerable<ContentEntity> newContents,
        Dictionary<string, CanonicalContent> canonicalMapping,
        IScoreCalculationService scoreService,
        CancellationToken cancellationToken = default)
    {
        var newContentsList = newContents?.ToList() ?? new List<ContentEntity>();
        if (!newContentsList.Any())
        {
            _logger.LogWarning("No content to synchronize");
            return new SyncResult(0, 0, 0, 0, 0);
        }

        // Start sync batch
        var syncBatch = SyncBatch.Start(
            newContentsList.Select(c => c.SourceProvider).Distinct().ToList());

        try
        {
            _logger.LogInformation(
                "Starting content sync for {Count} items from {Providers}",
                newContentsList.Count,
                string.Join(", ", syncBatch.SourceProviders));

            syncBatch.RecordItemsFetched(newContentsList.Count);

            // Step 1: Get existing content from database
            var contentIds = newContentsList.Select(c => c.Id).ToList();
            var existingContents = await _contentRepository.GetByIdsAsync(contentIds, cancellationToken);

            // Step 2: Detect changes
            var changeResults = await _changeDetectionStrategy.DetectChangesAsync(
                newContentsList,
                existingContents,
                cancellationToken);

            // Step 3: Group by change type
            var created = changeResults
                .Where(r => r.ChangeType == ChangeType.Created)
                .Select(r => r.Content)
                .ToList();

            var updated = changeResults
                .Where(r => r.ChangeType == ChangeType.Updated)
                .Select(r => r.Content)
                .ToList();

            var unchanged = changeResults
                .Where(r => r.ChangeType == ChangeType.None)
                .Select(r => r.Content)
                .ToList();

            syncBatch.RecordChangeResults(
                created.Count,
                updated.Count,
                unchanged.Count);

            _logger.LogInformation(
                "Change detection complete: {Created} new, {Updated} updated, {Unchanged} unchanged",
                created.Count,
                updated.Count,
                unchanged.Count);

            // Step 4: Calculate scores ONLY for changed items (optimization)
            _logger.LogInformation(
                "Calculating scores for {Count} changed items only",
                created.Count + updated.Count);

            foreach (var content in created.Concat(updated))
            {
                if (canonicalMapping.TryGetValue(content.Id, out var canonical))
                {
                    var calculatedScore = scoreService.CalculateScore(canonical);
                    content.UpdateScore(calculatedScore);
                    _logger.LogDebug(
                        "Score calculated for {ContentId}: {Score}",
                        content.Id, calculatedScore.Value);
                }
            }

            // Step 5: Prepare change logs
            var changeLogs = PrepareChangeLogs(changeResults, syncBatch.Id);

            // Step 6: Perform bulk operations
            var contentsToUpsert = created.Concat(updated).ToList();
            BulkOperationResult? bulkResult = null;

            if (contentsToUpsert.Any())
            {
                _logger.LogInformation(
                    "Performing bulk upsert for {Count} items",
                    contentsToUpsert.Count);

                bulkResult = await _bulkRepository.BulkUpsertWithBatchingAsync(
                    contentsToUpsert,
                    batchSize: 0, // 0 = auto-calculate based on content count
                    cancellationToken);

                syncBatch.RecordDatabaseResults(bulkResult.RowsAffected);

                _logger.LogInformation(
                    "Bulk operation result: {Summary}",
                    bulkResult.GetSummary());
            }
            else
            {
                _logger.LogInformation("No changes to persist");
                syncBatch.RecordDatabaseResults(0);
            }

            // Step 6: Save audit logs
            if (changeLogs.Any())
            {
                await _bulkRepository.SaveChangeLogsAsync(changeLogs, cancellationToken);
            }

            // Step 7: Complete sync batch
            syncBatch.CompleteSuccessfully();
            await _bulkRepository.SaveSyncBatchAsync(syncBatch, cancellationToken);

            return new SyncResult(
                newContentsList.Count,
                created.Count,
                updated.Count,
                unchanged.Count,
                bulkResult?.RowsAffected ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content sync failed");
            syncBatch.MarkAsFailed(ex.Message);
            await _bulkRepository.SaveSyncBatchAsync(syncBatch, cancellationToken);
            throw;
        }
    }

    private List<ContentChangeLog> PrepareChangeLogs(
        List<ChangeDetectionResult> changeResults,
        Guid syncBatchId)
    {
        var changeLogs = new List<ContentChangeLog>();

        foreach (var result in changeResults.Where(r => r.ChangeType != ChangeType.None))
        {
            var changedFieldsJson = result.ChangedFields.Any()
                ? System.Text.Json.JsonSerializer.Serialize(
                    result.ChangedFields.Select(f => new
                    {
                        Field = f.FieldName,
                        Old = f.OldValue,
                        New = f.NewValue
                    }))
                : null;

            var log = ContentChangeLog.Create(
                result.Content.Id,
                result.ChangeType,
                result.Content.ContentHash.Value,
                result.Content.Score.Value,
                result.Content.SourceProvider,
                syncBatchId,
                result.PreviousContent?.ContentHash.Value,
                result.PreviousContent?.Score.Value,
                changedFieldsJson);

            changeLogs.Add(log);
        }

        return changeLogs;
    }
}

/// <summary>
/// Result of content synchronization
/// </summary>
public sealed class SyncResult
{
    public int TotalProcessed { get; }
    public int Created { get; }
    public int Updated { get; }
    public int Unchanged { get; }
    public int DatabaseRowsAffected { get; }

    public SyncResult(
        int totalProcessed,
        int created,
        int updated,
        int unchanged,
        int databaseRowsAffected)
    {
        TotalProcessed = totalProcessed;
        Created = created;
        Updated = updated;
        Unchanged = unchanged;
        DatabaseRowsAffected = databaseRowsAffected;
    }

    public string GetSummary()
    {
        return $"Sync Result: Total={TotalProcessed}, " +
               $"Created={Created}, Updated={Updated}, " +
               $"Unchanged={Unchanged}, Rows={DatabaseRowsAffected}";
    }
}