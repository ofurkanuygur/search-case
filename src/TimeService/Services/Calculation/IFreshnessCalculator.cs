using TimeService.Domain.Entities;

namespace TimeService.Services.Calculation;

/// <summary>
/// Interface for calculating time-based freshness scores
/// SOLID: Single Responsibility - Only handles time-based scoring
/// SOLID: Dependency Inversion - Depend on abstraction, not concrete implementation
/// </summary>
public interface IFreshnessCalculator
{
    /// <summary>
    /// Calculates freshness score based on publication date
    ///
    /// Score thresholds:
    /// - ≤ 7 days: +5 points
    /// - ≤ 30 days: +3 points
    /// - ≤ 90 days: +1 point
    /// - > 90 days: +0 points
    /// </summary>
    /// <param name="content">Content entity to calculate freshness for</param>
    /// <returns>Freshness score (0-5)</returns>
    double CalculateFreshnessScore(ContentEntity content);

    /// <summary>
    /// Determines if content's freshness score would change today
    /// Used for optimization - only recalculate scores that will change
    /// </summary>
    /// <param name="publishedAt">Publication date</param>
    /// <returns>True if score transitions today (7, 30, or 90 days ago)</returns>
    bool IsFreshnessThresholdToday(DateTimeOffset publishedAt);
}
