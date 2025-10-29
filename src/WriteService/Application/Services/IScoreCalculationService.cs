using SearchCase.Contracts.Models;
using WriteService.Domain.ValueObjects;

namespace WriteService.Application.Services;

/// <summary>
/// Service for calculating content scores based on case study formula
/// </summary>
public interface IScoreCalculationService
{
    /// <summary>
    /// Calculates score for a content item
    /// </summary>
    /// <param name="content">Canonical content</param>
    /// <returns>Calculated score</returns>
    Score CalculateScore(CanonicalContent content);
}
