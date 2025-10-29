using WriteService.Domain.Entities;

namespace WriteService.Infrastructure.Scoring;

/// <summary>
/// Service for calculating content scores
/// </summary>
public interface IScoringService
{
    /// <summary>
    /// Calculate score for content using case formula
    /// </summary>
    /// <param name="content">Content entity to score</param>
    /// <returns>Calculated score</returns>
    double CalculateScore(ContentEntity content);
}
