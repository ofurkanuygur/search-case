using Hangfire;
using WriteService.Application.Interfaces;
using WriteService.Data.Repositories;
using WriteService.Domain.Entities;
using WriteService.Domain.ValueObjects;
using WriteService.Infrastructure.Scoring;
using WriteService.Infrastructure.EventBus;

namespace WriteService.Infrastructure.Jobs;

/// <summary>
/// Daily job to recalculate content scores using case formula
/// Runs at 02:00 UTC daily to update scores based on:
/// - Base score (views, likes, reading time, reactions)
/// - Freshness score (time-based decay)
/// - Engagement score (user interaction ratios)
///
/// Replaces FreshnessScoreUpdateJob with full score recalculation
/// </summary>
public sealed class ContentScoreUpdateJob
{
    private readonly IContentRepository _contentRepository;
    private readonly IBulkOperationRepository _bulkRepository;
    private readonly IScoringService _scoringService;
    private readonly IEventBusClient _eventBusClient;
    private readonly ILogger<ContentScoreUpdateJob> _logger;

    public ContentScoreUpdateJob(
        IContentRepository contentRepository,
        IBulkOperationRepository bulkRepository,
        IScoringService scoringService,
        IEventBusClient eventBusClient,
        ILogger<ContentScoreUpdateJob> logger)
    {
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
        _scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
        _eventBusClient = eventBusClient ?? throw new ArgumentNullException(nameof(eventBusClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute content score update
    /// Runs daily at 02:00 UTC via Hangfire recurring job
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 900)] // 15 minutes max
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Content Score Update Job");

        try
        {
            var startTime = DateTimeOffset.UtcNow;

            // Get all contents (paginated if needed for large datasets)
            var allContents = await _contentRepository.GetAllAsync(cancellationToken);

            _logger.LogInformation(
                "Loaded {Count} contents for score recalculation",
                allContents.Count);

            var updatedContents = new List<ContentEntity>();
            var unchangedCount = 0;

            // Recalculate scores for all content
            foreach (var content in allContents)
            {
                var oldScore = content.Score.Value;

                // Calculate new score using case formula
                var newScoreValue = _scoringService.CalculateScore(content);

                // Only update if score changed significantly (avoid unnecessary updates)
                if (Math.Abs(oldScore - (decimal)newScoreValue) > 0.01m)
                {
                    var newScore = Score.Create((decimal)newScoreValue);
                    content.UpdateScore(newScore);
                    updatedContents.Add(content);

                    _logger.LogDebug(
                        "Content {ContentId} score changed: {OldScore} → {NewScore} (Δ {Delta:+0.00;-0.00})",
                        content.Id, oldScore, newScoreValue, newScoreValue - (double)oldScore);
                }
                else
                {
                    unchangedCount++;
                }
            }

            // Bulk update scores if any changed
            if (updatedContents.Count > 0)
            {
                _logger.LogInformation(
                    "Updating {UpdatedCount} content scores (unchanged: {UnchangedCount})",
                    updatedContents.Count, unchangedCount);

                var bulkResult = await _bulkRepository.BulkUpdateScoresAsync(
                    updatedContents,
                    cancellationToken);

                if (bulkResult.IsSuccess)
                {
                    _logger.LogInformation(
                        "Successfully updated {Count} scores in database (rows affected: {Rows})",
                        updatedContents.Count,
                        bulkResult.RowsAffected);

                    // Publish events to update Redis & Elasticsearch
                    var updatedIds = updatedContents.Select(c => c.Id).ToList();

                    await _eventBusClient.PublishContentChangedAsync(
                        createdIds: new List<string>(), // No new content created
                        updatedIds: updatedIds,
                        sourceProvider: null,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation(
                        "Published {Count} content change events to update caches",
                        updatedIds.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to update {FailedCount} scores: {FailedIds}",
                        bulkResult.FailedIds.Count,
                        string.Join(", ", bulkResult.FailedIds));
                }
            }
            else
            {
                _logger.LogInformation("No score changes detected, skipping database update");
            }

            var duration = DateTimeOffset.UtcNow - startTime;

            _logger.LogInformation(
                "Content Score Update Job completed in {Duration}s: " +
                "Total={Total}, Updated={Updated}, Unchanged={Unchanged}",
                duration.TotalSeconds,
                allContents.Count,
                updatedContents.Count,
                unchangedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content Score Update Job failed");
            throw;
        }
    }
}
