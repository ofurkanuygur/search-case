using FluentValidation;
using SearchCase.Contracts.Mapping;
using SearchCase.Contracts.Models;
using XmlProviderMicroservice.Models;

namespace XmlProviderMicroservice.Mapping;

/// <summary>
/// Maps XML provider items to canonical content (video or article)
/// </summary>
public sealed class XmlToCanonicalMapper : IContentMapper<Item>
{
    private readonly IValidator<CanonicalVideoContent> _videoValidator;
    private readonly IValidator<CanonicalArticleContent> _articleValidator;
    private readonly ILogger<XmlToCanonicalMapper> _logger;

    public XmlToCanonicalMapper(
        IValidator<CanonicalVideoContent> videoValidator,
        IValidator<CanonicalArticleContent> articleValidator,
        ILogger<XmlToCanonicalMapper> logger)
    {
        _videoValidator = videoValidator;
        _articleValidator = articleValidator;
        _logger = logger;
    }

    public MappingResult<CanonicalContent> MapToCanonical(Item source, string providerId)
    {
        try
        {
            // Route based on content type
            CanonicalContent canonical = source.Type.ToLowerInvariant() switch
            {
                "video" => MapToVideo(source, providerId),
                "article" => MapToArticle(source, providerId),
                _ => throw new NotSupportedException($"Content type '{source.Type}' is not supported")
            };

            // Validate based on type
            var validationResult = canonical switch
            {
                CanonicalVideoContent video => _videoValidator.Validate(video),
                CanonicalArticleContent article => _articleValidator.Validate(article),
                _ => throw new InvalidOperationException("Unknown canonical content type")
            };

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToList();

                _logger.LogWarning(
                    "Validation failed for content {ContentId}: {Errors}",
                    source.Id,
                    string.Join(", ", errors));

                return MappingResult<CanonicalContent>.Fail(errors);
            }

            return MappingResult<CanonicalContent>.Ok(canonical);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map content {ContentId}", source.Id);
            return MappingResult<CanonicalContent>.Fail($"Mapping error: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps XML item to canonical video content
    /// </summary>
    private CanonicalVideoContent MapToVideo(Item source, string providerId)
    {
        return new CanonicalVideoContent
        {
            Id = source.Id,
            Title = source.Headline,
            PublishedAt = DateTimeOffset.Parse(source.PublicationDate),
            Categories = source.Categories,
            SourceProvider = providerId,
            TransformedAt = DateTimeOffset.UtcNow,
            Metrics = new VideoMetrics
            {
                Views = source.Stats.Views ?? 0,
                Likes = source.Stats.Likes ?? 0,
                Duration = ParseDuration(source.Stats.Duration ?? "00:00")
            }
        };
    }

    /// <summary>
    /// Maps XML item to canonical article content
    /// </summary>
    private CanonicalArticleContent MapToArticle(Item source, string providerId)
    {
        return new CanonicalArticleContent
        {
            Id = source.Id,
            Title = source.Headline,
            PublishedAt = DateTimeOffset.Parse(source.PublicationDate),
            Categories = source.Categories,
            SourceProvider = providerId,
            TransformedAt = DateTimeOffset.UtcNow,
            Metrics = new ArticleMetrics
            {
                ReadingTimeMinutes = source.Stats.ReadingTime ?? 0,
                Reactions = source.Stats.Reactions ?? 0,
                Comments = source.Stats.Comments ?? 0
            }
        };
    }

    /// <summary>
    /// Parses duration from "MM:SS" format to TimeSpan
    /// </summary>
    private static TimeSpan ParseDuration(string duration)
    {
        var parts = duration.Split(':');
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid duration format: {duration}. Expected MM:SS");
        }

        var minutes = int.Parse(parts[0]);
        var seconds = int.Parse(parts[1]);

        return new TimeSpan(0, minutes, seconds);
    }
}
