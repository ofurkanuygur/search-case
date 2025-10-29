using TimeService.Models;

namespace TimeService.Services.Orchestration;

/// <summary>
/// Orchestrator interface for coordinating score update operations
/// SOLID: Single Responsibility - Coordinates score updates
/// SOLID: Dependency Inversion - Depend on abstraction
/// </summary>
public interface ITimeServiceOrchestrator
{
    /// <summary>
    /// Updates scores for all content crossing freshness thresholds today
    /// This is the main entry point called by DailyJob
    ///
    /// Process:
    /// 1. Identify content crossing thresholds (7, 30, or 90 days old)
    /// 2. Recalculate freshness scores
    /// 3. Recalculate final scores using appropriate strategy (Video/Article)
    /// 4. Bulk update database
    /// 5. Publish events to EventBus for downstream services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the update</returns>
    Task<ScoreUpdateResult> UpdateDailyScoresAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces recalculation of all content scores (for manual trigger/testing)
    /// WARNING: This is expensive - only use for initial setup or manual correction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the update</returns>
    Task<ScoreUpdateResult> RecalculateAllScoresAsync(CancellationToken cancellationToken = default);
}
