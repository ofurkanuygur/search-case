using TimeService.Domain.Entities;

namespace TimeService.Services.Calculation;

/// <summary>
/// Time-based freshness score calculator
/// Implements decay algorithm based on content age
///
/// SOLID: Single Responsibility - Only calculates time-based freshness
/// SOLID: Open/Closed - Can be extended with different decay algorithms
/// </summary>
public sealed class FreshnessCalculator : IFreshnessCalculator
{
    private const int Threshold7Days = 7;
    private const int Threshold30Days = 30;
    private const int Threshold90Days = 90;

    private const double Score7Days = 5.0;
    private const double Score30Days = 3.0;
    private const double Score90Days = 1.0;
    private const double ScoreOlder = 0.0;

    private readonly ILogger<FreshnessCalculator> _logger;

    public FreshnessCalculator(ILogger<FreshnessCalculator> logger)
    {
        _logger = logger;
    }

    public double CalculateFreshnessScore(ContentEntity content)
    {
        var daysSincePublication = (DateTimeOffset.UtcNow - content.PublishedAt).TotalDays;

        var score = daysSincePublication switch
        {
            <= Threshold7Days => Score7Days,    // Fresh content: +5
            <= Threshold30Days => Score30Days,  // Recent content: +3
            <= Threshold90Days => Score90Days,  // Aging content: +1
            _ => ScoreOlder                     // Old content: +0
        };

        _logger.LogDebug(
            "Freshness score calculated for {ContentId}: {Score} points (age: {Days:F1} days, published: {Published})",
            content.Id, score, daysSincePublication, content.PublishedAt);

        return score;
    }

    public bool IsFreshnessThresholdToday(DateTimeOffset publishedAt)
    {
        var daysSincePublication = (int)(DateTimeOffset.UtcNow - publishedAt).TotalDays;

        // Check if content crossed a threshold today
        // This is critical for optimization - only update scores that actually change
        var isThreshold = daysSincePublication == Threshold7Days ||
                         daysSincePublication == Threshold30Days ||
                         daysSincePublication == Threshold90Days;

        if (isThreshold)
        {
            _logger.LogDebug(
                "Content published {Days} days ago crosses freshness threshold today (published: {Published})",
                daysSincePublication, publishedAt);
        }

        return isThreshold;
    }

    /// <summary>
    /// Gets content IDs that need score updates today
    /// Performance optimization: Only recalculate scores for content crossing thresholds
    /// </summary>
    /// <param name="allContent">All content in database</param>
    /// <returns>Content entities that need score updates</returns>
    public IEnumerable<ContentEntity> GetContentNeedingUpdate(IEnumerable<ContentEntity> allContent)
    {
        var today = DateTimeOffset.UtcNow.Date;

        // Get content published exactly 7, 30, or 90 days ago (threshold crossings)
        var threshold7Date = today.AddDays(-Threshold7Days);
        var threshold30Date = today.AddDays(-Threshold30Days);
        var threshold90Date = today.AddDays(-Threshold90Days);

        var contentNeedingUpdate = allContent
            .Where(c =>
                c.PublishedAt.Date == threshold7Date ||
                c.PublishedAt.Date == threshold30Date ||
                c.PublishedAt.Date == threshold90Date)
            .ToList();

        _logger.LogInformation(
            "Found {Count} content items crossing freshness thresholds today " +
            "(7d: {Threshold7}, 30d: {Threshold30}, 90d: {Threshold90})",
            contentNeedingUpdate.Count,
            threshold7Date.ToString("yyyy-MM-dd"),
            threshold30Date.ToString("yyyy-MM-dd"),
            threshold90Date.ToString("yyyy-MM-dd"));

        return contentNeedingUpdate;
    }
}
