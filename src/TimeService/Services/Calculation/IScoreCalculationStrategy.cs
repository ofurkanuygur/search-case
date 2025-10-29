using TimeService.Domain.Entities;
using TimeService.Domain.ValueObjects;

namespace TimeService.Services.Calculation;

/// <summary>
/// Strategy pattern interface for score calculation
/// Allows different calculation algorithms for different content types
/// SOLID: Open/Closed Principle - Open for extension via new strategies
/// </summary>
public interface IScoreCalculationStrategy
{
    /// <summary>
    /// Calculates the final score for a content entity
    /// Formula: (Base Score × Content Type Multiplier) + Freshness Score + Engagement Score
    /// </summary>
    /// <param name="content">The content entity to calculate score for</param>
    /// <param name="freshnessScore">Pre-calculated freshness score from IFreshnessCalculator</param>
    /// <returns>Calculated Score value object</returns>
    Score CalculateFinalScore(ContentEntity content, double freshnessScore);

    /// <summary>
    /// Determines if this strategy can handle the given content
    /// SOLID: Single Responsibility - Each strategy knows its own applicability
    /// </summary>
    /// <param name="content">Content to check</param>
    /// <returns>True if this strategy can calculate score for the content</returns>
    bool CanHandle(ContentEntity content);
}
