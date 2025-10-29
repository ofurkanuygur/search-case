using EventBusContracts;
using TimeService.Infrastructure.Repository;
using TimeService.Models;
using TimeService.Services.Calculation;
using TimeService.Services.EventBus;

namespace TimeService.Services.Orchestration;

/// <summary>
/// Main orchestrator for coordinating score update operations
/// Implements SOLID principles:
/// - Single Responsibility: Coordinates score updates only
/// - Open/Closed: Open for new strategies, closed for modification
/// - Liskov Substitution: All strategies are interchangeable
/// - Interface Segregation: Depends only on needed interfaces
/// - Dependency Inversion: Depends on abstractions, not concretions
/// </summary>
public sealed class TimeServiceOrchestrator : ITimeServiceOrchestrator
{
    private readonly IScoreRepository _repository;
    private readonly IFreshnessCalculator _freshnessCalculator;
    private readonly IEnumerable<IScoreCalculationStrategy> _scoreStrategies;
    private readonly IEventBusClient _eventBusClient;
    private readonly ILogger<TimeServiceOrchestrator> _logger;

    public TimeServiceOrchestrator(
        IScoreRepository repository,
        IFreshnessCalculator freshnessCalculator,
        IEnumerable<IScoreCalculationStrategy> scoreStrategies,
        IEventBusClient eventBusClient,
        ILogger<TimeServiceOrchestrator> logger)
    {
        _repository = repository;
        _freshnessCalculator = freshnessCalculator;
        _scoreStrategies = scoreStrategies;
        _eventBusClient = eventBusClient;
        _logger = logger;
    }

    public async Task<ScoreUpdateResult> UpdateDailyScoresAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Starting daily score update at {Time}", startedAt);

        try
        {
            // Step 1: Get only content crossing freshness thresholds today (OPTIMIZATION!)
            var thresholdDates = GetFreshnessThresholdDates();
            var contentToUpdate = await _repository.GetContentByPublishDatesAsync(
                thresholdDates,
                cancellationToken);

            if (contentToUpdate.Count == 0)
            {
                _logger.LogInformation("No content crossing freshness thresholds today");
                return ScoreUpdateResult.Success(0, 0, 0, new List<string>(), startedAt, DateTimeOffset.UtcNow);
            }

            _logger.LogInformation(
                "Found {Count} content items crossing freshness thresholds: " +
                "7d={Date7}, 30d={Date30}, 90d={Date90}",
                contentToUpdate.Count,
                thresholdDates[0].ToString("yyyy-MM-dd"),
                thresholdDates[1].ToString("yyyy-MM-dd"),
                thresholdDates[2].ToString("yyyy-MM-dd"));

            // Step 2: Recalculate scores
            var updatedContent = new List<TimeService.Domain.Entities.ContentEntity>();
            var updatedIds = new List<string>();

            foreach (var content in contentToUpdate)
            {
                try
                {
                    // Calculate freshness score
                    var freshnessScore = _freshnessCalculator.CalculateFreshnessScore(content);

                    // Find appropriate strategy (Video or Article)
                    var strategy = _scoreStrategies.FirstOrDefault(s => s.CanHandle(content));
                    if (strategy == null)
                    {
                        _logger.LogWarning(
                            "No scoring strategy found for content {ContentId} type {Type}",
                            content.Id,
                            content.Type);
                        continue;
                    }

                    // Calculate final score
                    var newScore = strategy.CalculateFinalScore(content, freshnessScore);

                    // Update score (uses domain method)
                    content.UpdateScore(newScore);

                    updatedContent.Add(content);
                    updatedIds.Add(content.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating score for content {ContentId}", content.Id);
                }
            }

            // Step 3: Bulk update database
            var affectedRows = await _repository.BulkUpdateScoresAsync(updatedContent, cancellationToken);

            _logger.LogInformation(
                "Database update completed: {Affected} rows affected out of {Total} calculated",
                affectedRows,
                updatedContent.Count);

            // Step 4: Publish events to EventBus for downstream services (Cache + Search workers)
            if (updatedIds.Count > 0)
            {
                await PublishScoreUpdatedEventAsync(updatedIds, cancellationToken);
            }

            var completedAt = DateTimeOffset.UtcNow;

            return ScoreUpdateResult.Success(
                contentToUpdate.Count,
                affectedRows,
                contentToUpdate.Count - affectedRows,
                updatedIds,
                startedAt,
                completedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during daily score update");
            var completedAt = DateTimeOffset.UtcNow;
            return ScoreUpdateResult.Failed(0, 0, 1, startedAt, completedAt);
        }
    }

    public async Task<ScoreUpdateResult> RecalculateAllScoresAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        _logger.LogWarning("Starting FULL score recalculation (expensive operation) at {Time}", startedAt);

        try
        {
            // Get ALL content
            var allContent = await _repository.GetAllContentAsync(cancellationToken);

            _logger.LogInformation("Recalculating scores for {Count} content items", allContent.Count);

            var updatedContent = new List<TimeService.Domain.Entities.ContentEntity>();
            var updatedIds = new List<string>();
            var errors = 0;

            foreach (var content in allContent)
            {
                try
                {
                    var freshnessScore = _freshnessCalculator.CalculateFreshnessScore(content);
                    var strategy = _scoreStrategies.FirstOrDefault(s => s.CanHandle(content));

                    if (strategy == null)
                    {
                        _logger.LogWarning("No strategy for content {ContentId}", content.Id);
                        errors++;
                        continue;
                    }

                    var newScore = strategy.CalculateFinalScore(content, freshnessScore);
                    content.UpdateScore(newScore);

                    updatedContent.Add(content);
                    updatedIds.Add(content.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recalculating score for {ContentId}", content.Id);
                    errors++;
                }
            }

            var affectedRows = await _repository.BulkUpdateScoresAsync(updatedContent, cancellationToken);

            if (updatedIds.Count > 0)
            {
                await PublishScoreUpdatedEventAsync(updatedIds, cancellationToken);
            }

            var completedAt = DateTimeOffset.UtcNow;

            return errors > 0
                ? ScoreUpdateResult.Failed(allContent.Count, affectedRows, errors, startedAt, completedAt)
                : ScoreUpdateResult.Success(allContent.Count, affectedRows, 0, updatedIds, startedAt, completedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during full recalculation");
            var completedAt = DateTimeOffset.UtcNow;
            return ScoreUpdateResult.Failed(0, 0, 1, startedAt, completedAt);
        }
    }

    /// <summary>
    /// Gets the dates that represent freshness threshold crossings
    /// Returns: [7 days ago, 30 days ago, 90 days ago]
    /// </summary>
    private List<DateTimeOffset> GetFreshnessThresholdDates()
    {
        var today = DateTimeOffset.UtcNow.Date;
        return new List<DateTimeOffset>
        {
            today.AddDays(-7),   // Content turning 7 days old today (5 → 3 points)
            today.AddDays(-30),  // Content turning 30 days old today (3 → 1 point)
            today.AddDays(-90)   // Content turning 90 days old today (1 → 0 points)
        };
    }

    /// <summary>
    /// Publishes ContentBatchUpdatedEvent to EventBus with ChangeType="ScoreUpdated"
    /// Triggers cache upsert (CacheWorker) and search index update (SearchWorker)
    /// Uses existing /api/events/batch endpoint, same as WriteService
    /// </summary>
    private async Task PublishScoreUpdatedEventAsync(
        List<string> updatedContentIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = new ContentBatchUpdatedEvent
            {
                ContentIds = updatedContentIds,
                ChangeType = "ScoreUpdated", // Differentiate from content changes
                SourceProvider = "TimeService",
                ProcessedAt = DateTimeOffset.UtcNow,
                BatchId = Guid.NewGuid()
            };

            await _eventBusClient.PublishScoreUpdatesAsync(@event, cancellationToken);

            _logger.LogInformation(
                "Published ContentBatchUpdatedEvent (ScoreUpdated) for {Count} content items to EventBus (BatchId: {BatchId})",
                updatedContentIds.Count,
                @event.BatchId);
        }
        catch (Exception ex)
        {
            // Don't fail the entire operation if event publishing fails
            _logger.LogError(ex, "Failed to publish ContentBatchUpdatedEvent to EventBus");
        }
    }
}
