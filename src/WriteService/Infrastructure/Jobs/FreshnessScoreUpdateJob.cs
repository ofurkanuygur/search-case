using Hangfire;
using Microsoft.Extensions.Logging;
using WriteService.Application.Interfaces;
using WriteService.Data.Repositories;
using WriteService.Domain.Entities;
using WriteService.Domain.ValueObjects;

namespace WriteService.Infrastructure.Jobs;

/// <summary>
/// Daily job to update freshness/recency scores for content
/// Runs at boundaries (7, 30, 90 days) where scores change
/// As per theoretical diagram: "Freshness Score Update Job (Daily 02:00)"
/// </summary>
public sealed class FreshnessScoreUpdateJob
{
    private readonly IContentRepository _contentRepository;
    private readonly IBulkOperationRepository _bulkRepository;
    private readonly ILogger<FreshnessScoreUpdateJob> _logger;

    // Recency boundaries in days (where scores change)
    private static readonly int[] RecencyBoundaries = { 7, 30, 90 };

    public FreshnessScoreUpdateJob(
        IContentRepository contentRepository,
        IBulkOperationRepository bulkRepository,
        ILogger<FreshnessScoreUpdateJob> logger)
    {
        _contentRepository = contentRepository;
        _bulkRepository = bulkRepository;
        _logger = logger;
    }

    /// <summary>
    /// Execute freshness score update
    /// This should run daily at 02:00 UTC
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 900)] // Prevent overlapping executions
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Freshness Score Update Job");

        try
        {
            var updatedCount = 0;
            var checkedCount = 0;

            // Process each recency boundary
            foreach (var boundaryDays in RecencyBoundaries)
            {
                var boundaryDate = DateTimeOffset.UtcNow.AddDays(-boundaryDays);
                var boundaryDateEnd = boundaryDate.AddDays(-2); // 2-day window

                _logger.LogInformation(
                    "Checking content at {Days}-day boundary ({StartDate} to {EndDate})",
                    boundaryDays,
                    boundaryDateEnd.ToString("yyyy-MM-dd"),
                    boundaryDate.ToString("yyyy-MM-dd"));

                // Get contents near the boundary
                var contents = await _contentRepository.GetByDateRangeAsync(
                    boundaryDateEnd,
                    boundaryDate,
                    cancellationToken);

                if (contents.Count == 0)
                {
                    _logger.LogDebug("No content found at {Days}-day boundary", boundaryDays);
                    continue;
                }

                checkedCount += contents.Count;
                _logger.LogInformation(
                    "Found {Count} contents at {Days}-day boundary",
                    contents.Count, boundaryDays);

                // Calculate which ones need score updates
                var toUpdate = new List<ContentEntity>();

                foreach (var content in contents)
                {
                    // Get the canonical content to recalculate score
                    var oldScore = content.Score.Value;

                    // Simulate recalculation (in real scenario, would fetch canonical data)
                    var daysSincePublication = (DateTimeOffset.UtcNow - content.PublishedAt).TotalDays;
                    var recencyScoreChange = CalculateRecencyScoreChange(daysSincePublication, boundaryDays);

                    if (Math.Abs(recencyScoreChange) > 0.01m) // Only update if score actually changes
                    {
                        var newScore = Score.Create(oldScore + recencyScoreChange);
                        content.UpdateScore(newScore);
                        toUpdate.Add(content);

                        _logger.LogDebug(
                            "Content {ContentId} score updated: {OldScore} -> {NewScore} (change: {Change})",
                            content.Id, oldScore, newScore.Value, recencyScoreChange);
                    }
                }

                // Bulk update scores if any changes
                if (toUpdate.Count > 0)
                {
                    _logger.LogInformation(
                        "Updating {Count} scores at {Days}-day boundary",
                        toUpdate.Count, boundaryDays);

                    // Use BulkUpdateScoresAsync instead of BulkUpsertWithBatchingAsync
                    // This ensures updated_at is always updated, regardless of content_hash
                    var bulkResult = await _bulkRepository.BulkUpdateScoresAsync(
                        toUpdate,
                        cancellationToken);

                    if (bulkResult.IsSuccess)
                    {
                        updatedCount += toUpdate.Count;
                        _logger.LogInformation(
                            "Successfully updated {Count} scores (rows affected: {Rows})",
                            toUpdate.Count,
                            bulkResult.RowsAffected);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to update {FailedCount} scores",
                            bulkResult.FailedIds.Count);
                    }
                }
            }

            _logger.LogInformation(
                "Freshness Score Update Job completed: Checked={Checked}, Updated={Updated}",
                checkedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Freshness Score Update Job failed");
            throw;
        }
    }

    /// <summary>
    /// Calculate the recency score change at boundaries
    /// </summary>
    private decimal CalculateRecencyScoreChange(double daysSincePublication, int boundary)
    {
        // Score changes at boundaries:
        // 0-7 days: 5 points
        // 7-30 days: 3 points (change: -2)
        // 30-90 days: 1 point (change: -2)
        // 90+ days: 0 points (change: -1)

        return boundary switch
        {
            7 when daysSincePublication >= 7 && daysSincePublication < 9 => -2m,   // 5 -> 3
            30 when daysSincePublication >= 30 && daysSincePublication < 32 => -2m, // 3 -> 1
            90 when daysSincePublication >= 90 && daysSincePublication < 92 => -1m,  // 1 -> 0
            _ => 0m
        };
    }
}