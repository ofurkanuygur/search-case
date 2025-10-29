using System.Text.Json;
using TimeService.Domain.Entities;
using TimeService.Domain.Enums;
using TimeService.Domain.ValueObjects;

namespace TimeService.Services.Calculation;

/// <summary>
/// Article content score calculation strategy
/// Implements the specific formula for article/text content scoring
///
/// Formula:
/// - Base Score = reading_time + (reactions / 50)
/// - Content Type Multiplier = 1.0
/// - Engagement Score = (reactions / reading_time) × 5
/// - Final Score = (Base Score × 1.0) + Freshness Score + Engagement Score
///
/// SOLID: Single Responsibility - Only handles article score calculation
/// </summary>
public sealed class ArticleScoreStrategy : IScoreCalculationStrategy
{
    private const double ContentTypeMultiplier = 1.0;
    private const double EngagementMultiplier = 5.0;
    private readonly ILogger<ArticleScoreStrategy> _logger;

    public ArticleScoreStrategy(ILogger<ArticleScoreStrategy> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(ContentEntity content)
    {
        return content.Type == ContentType.Article;
    }

    public Score CalculateFinalScore(ContentEntity content, double freshnessScore)
    {
        if (!CanHandle(content))
        {
            throw new InvalidOperationException(
                $"ArticleScoreStrategy cannot handle content type: {content.Type}");
        }

        try
        {
            // Extract metrics from JSONB (database uses PascalCase keys)
            var metrics = content.Metrics;
            var readingTime = GetMetricValue(metrics, "ReadingTimeMinutes");
            var reactions = GetMetricValue(metrics, "Reactions");

            // Calculate Base Score: reading_time + (reactions / 50)
            var baseScore = readingTime + (reactions / 50.0);

            // Calculate Engagement Score: (reactions / reading_time) × 5
            var engagementScore = readingTime > 0
                ? (reactions / (double)readingTime) * EngagementMultiplier
                : 0;

            // Final Score = (Base Score × Content Type Multiplier) + Freshness Score + Engagement Score
            var finalScore = (baseScore * ContentTypeMultiplier) + freshnessScore + engagementScore;

            _logger.LogDebug(
                "Article score calculated for {ContentId}: Base={Base:F2}, Engagement={Engagement:F2}, " +
                "Freshness={Freshness:F2}, Final={Final:F2} (readingTime={ReadingTime}, reactions={Reactions})",
                content.Id, baseScore, engagementScore, freshnessScore, finalScore, readingTime, reactions);

            return Score.Create((decimal)finalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating article score for {ContentId}", content.Id);
            return Score.Zero;
        }
    }

    private static long GetMetricValue(JsonElement metrics, string propertyName)
    {
        if (metrics.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt64();
            }
        }
        return 0;
    }
}
