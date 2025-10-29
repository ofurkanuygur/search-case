using System.Text.Json;
using TimeService.Domain.Entities;
using TimeService.Domain.Enums;
using TimeService.Domain.ValueObjects;

namespace TimeService.Services.Calculation;

/// <summary>
/// Video content score calculation strategy
/// Implements the specific formula for video content scoring
///
/// Formula:
/// - Base Score = (views / 1000) + (likes / 100)
/// - Content Type Multiplier = 1.5
/// - Engagement Score = (likes / views) × 10
/// - Final Score = (Base Score × 1.5) + Freshness Score + Engagement Score
///
/// SOLID: Single Responsibility - Only handles video score calculation
/// </summary>
public sealed class VideoScoreStrategy : IScoreCalculationStrategy
{
    private const double ContentTypeMultiplier = 1.5;
    private const double EngagementMultiplier = 10.0;
    private readonly ILogger<VideoScoreStrategy> _logger;

    public VideoScoreStrategy(ILogger<VideoScoreStrategy> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(ContentEntity content)
    {
        return content.Type == ContentType.Video;
    }

    public Score CalculateFinalScore(ContentEntity content, double freshnessScore)
    {
        if (!CanHandle(content))
        {
            throw new InvalidOperationException(
                $"VideoScoreStrategy cannot handle content type: {content.Type}");
        }

        try
        {
            // Extract metrics from JSONB (database uses PascalCase keys)
            var metrics = content.Metrics;
            var views = GetMetricValue(metrics, "Views");
            var likes = GetMetricValue(metrics, "Likes");

            // Calculate Base Score: (views / 1000) + (likes / 100)
            var baseScore = (views / 1000.0) + (likes / 100.0);

            // Calculate Engagement Score: (likes / views) × 10
            var engagementScore = views > 0
                ? (likes / views) * EngagementMultiplier
                : 0;

            // Final Score = (Base Score × Content Type Multiplier) + Freshness Score + Engagement Score
            var finalScore = (baseScore * ContentTypeMultiplier) + freshnessScore + engagementScore;

            _logger.LogDebug(
                "Video score calculated for {ContentId}: Base={Base:F2}, Engagement={Engagement:F2}, " +
                "Freshness={Freshness:F2}, Final={Final:F2} (views={Views}, likes={Likes})",
                content.Id, baseScore, engagementScore, freshnessScore, finalScore, views, likes);

            return Score.Create((decimal)finalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating video score for {ContentId}", content.Id);
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
