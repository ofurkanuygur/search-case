using System.Text.Json;
using WriteService.Domain.Entities;
using WriteService.Domain.Enums;

namespace WriteService.Infrastructure.Scoring;

/// <summary>
/// Scoring service implementing case-specific formula
///
/// Formula:
/// Final Score = (Base Score × Content Type Coefficient) + Freshness Score + Engagement Score
///
/// Base Score:
/// - Video: views/1000 + likes/100
/// - Article: reading_time + reactions/50
///
/// Content Type Coefficient:
/// - Video: 1.5
/// - Article: 1.0
///
/// Freshness Score:
/// - < 1 week: +5
/// - < 1 month: +3
/// - < 3 months: +1
/// - Older: 0
///
/// Engagement Score:
/// - Video: (likes/views) × 10
/// - Article: (reactions/reading_time) × 5
/// </summary>
public sealed class CaseScoringService : IScoringService
{
    private readonly ILogger<CaseScoringService> _logger;

    public CaseScoringService(ILogger<CaseScoringService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public double CalculateScore(ContentEntity content)
    {
        try
        {
            var baseScore = CalculateBaseScore(content);
            var typeCoefficient = GetTypeCoefficient(content.Type);
            var freshnessScore = CalculateFreshnessScore(content.PublishedAt);
            var engagementScore = CalculateEngagementScore(content);

            var finalScore = (baseScore * typeCoefficient) + freshnessScore + engagementScore;

            _logger.LogDebug(
                "Calculated score for {ContentId}: base={Base}, coefficient={Coefficient}, " +
                "freshness={Freshness}, engagement={Engagement}, final={Final}",
                content.Id, baseScore, typeCoefficient, freshnessScore, engagementScore, finalScore
            );

            return Math.Round(finalScore, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating score for content {ContentId}", content.Id);
            return 0.0; // Return default score on error
        }
    }

    /// <summary>
    /// Calculate base score based on content type
    /// Video: views/1000 + likes/100
    /// Article: reading_time + reactions/50
    /// </summary>
    private double CalculateBaseScore(ContentEntity content)
    {
        if (content.Type == ContentType.Video)
        {
            var metrics = DeserializeMetrics<VideoMetrics>(content.Metrics);
            return (metrics.Views / 1000.0) + (metrics.Likes / 100.0);
        }
        else // article
        {
            var metrics = DeserializeMetrics<ArticleMetrics>(content.Metrics);
            return metrics.ReadingTimeMinutes + (metrics.Reactions / 50.0);
        }
    }

    /// <summary>
    /// Get content type coefficient
    /// Video: 1.5, Article: 1.0
    /// </summary>
    private static double GetTypeCoefficient(ContentType type)
    {
        return type switch
        {
            ContentType.Video => 1.5,
            ContentType.Article => 1.0,
            _ => 1.0
        };
    }

    /// <summary>
    /// Calculate freshness score based on age
    /// < 1 week: +5, < 1 month: +3, < 3 months: +1, Older: 0
    /// </summary>
    private static double CalculateFreshnessScore(DateTimeOffset publishedAt)
    {
        var age = DateTimeOffset.UtcNow - publishedAt;

        if (age.TotalDays <= 7)
            return 5.0;
        else if (age.TotalDays <= 30)
            return 3.0;
        else if (age.TotalDays <= 90)
            return 1.0;
        else
            return 0.0;
    }

    /// <summary>
    /// Calculate engagement score based on content type
    /// Video: (likes/views) × 10
    /// Article: (reactions/reading_time) × 5
    /// </summary>
    private double CalculateEngagementScore(ContentEntity content)
    {
        if (content.Type == ContentType.Video)
        {
            var metrics = DeserializeMetrics<VideoMetrics>(content.Metrics);

            if (metrics.Views == 0) return 0.0;

            return (metrics.Likes / (double)metrics.Views) * 10.0;
        }
        else // article
        {
            var metrics = DeserializeMetrics<ArticleMetrics>(content.Metrics);

            if (metrics.ReadingTimeMinutes == 0) return 0.0;

            return (metrics.Reactions / (double)metrics.ReadingTimeMinutes) * 5.0;
        }
    }

    private T DeserializeMetrics<T>(JsonElement metrics) where T : new()
    {
        try
        {
            var json = metrics.GetRawText();
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize metrics to {Type}", typeof(T).Name);
            return new T();
        }
    }

    // Helper classes for metrics deserialization
    private sealed class VideoMetrics
    {
        public int Views { get; set; }
        public int Likes { get; set; }
        public int Duration { get; set; }
    }

    private sealed class ArticleMetrics
    {
        public int ReadingTimeMinutes { get; set; }
        public int Reactions { get; set; }
        public int Comments { get; set; }
    }
}
