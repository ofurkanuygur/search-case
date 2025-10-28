using Microsoft.Extensions.Logging;
using SearchCase.Contracts.Models;
using WriteService.Domain.ValueObjects;

namespace WriteService.Application.Services;

/// <summary>
/// Implementation of score calculation using WEG case study formulas
/// </summary>
public sealed class ScoreCalculationService : IScoreCalculationService
{
    private readonly ILogger<ScoreCalculationService> _logger;

    public ScoreCalculationService(ILogger<ScoreCalculationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Score CalculateScore(CanonicalContent content)
    {
        var score = content switch
        {
            CanonicalVideoContent video => CalculateVideoScore(video),
            CanonicalArticleContent article => CalculateArticleScore(article),
            _ => throw new NotSupportedException($"Content type {content.GetType().Name} not supported")
        };

        return Score.Create(score);
    }

    private decimal CalculateVideoScore(CanonicalVideoContent video)
    {
        // Base Score: (views/1000 + likes/100) × 1.5
        var baseScore = ((video.Metrics.Views / 1000m) + (video.Metrics.Likes / 100m)) * 1.5m;

        // Recency Score
        var recencyScore = CalculateRecencyScore(video.PublishedAt);

        // Engagement Score: (likes/views) × 10
        var engagementScore = video.Metrics.Views > 0
            ? (video.Metrics.Likes / (decimal)video.Metrics.Views) * 10m
            : 0m;

        var totalScore = baseScore + recencyScore + engagementScore;

        _logger.LogDebug(
            "Video {Id} score: Base={Base}, Recency={Recency}, Engagement={Engagement}, Total={Total}",
            video.Id, baseScore, recencyScore, engagementScore, totalScore);

        return totalScore;
    }

    private decimal CalculateArticleScore(CanonicalArticleContent article)
    {
        // Base Score: (reading_time + reactions/50) × 1.0
        var baseScore = (article.Metrics.ReadingTimeMinutes + (article.Metrics.Reactions / 50m)) * 1.0m;

        // Recency Score
        var recencyScore = CalculateRecencyScore(article.PublishedAt);

        // Engagement Score: (reactions/reading_time) × 5
        var engagementScore = article.Metrics.ReadingTimeMinutes > 0
            ? (article.Metrics.Reactions / (decimal)article.Metrics.ReadingTimeMinutes) * 5m
            : 0m;

        var totalScore = baseScore + recencyScore + engagementScore;

        _logger.LogDebug(
            "Article {Id} score: Base={Base}, Recency={Recency}, Engagement={Engagement}, Total={Total}",
            article.Id, baseScore, recencyScore, engagementScore, totalScore);

        return totalScore;
    }

    private decimal CalculateRecencyScore(DateTimeOffset publishedAt)
    {
        var daysSincePublication = (DateTimeOffset.UtcNow - publishedAt).TotalDays;

        return daysSincePublication switch
        {
            <= 7 => 5m,
            <= 30 => 3m,
            <= 90 => 1m,
            _ => 0m
        };
    }
}
