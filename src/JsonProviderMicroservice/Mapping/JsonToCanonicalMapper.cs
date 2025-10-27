using FluentValidation;
using JsonProviderMicroservice.Models;
using SearchCase.Contracts.Mapping;
using SearchCase.Contracts.Models;

namespace JsonProviderMicroservice.Mapping;

/// <summary>
/// Maps JSON provider content to canonical video content
/// </summary>
public sealed class JsonToCanonicalMapper : IContentMapper<Content>
{
    private readonly IValidator<CanonicalVideoContent> _validator;
    private readonly ILogger<JsonToCanonicalMapper> _logger;

    public JsonToCanonicalMapper(
        IValidator<CanonicalVideoContent> validator,
        ILogger<JsonToCanonicalMapper> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public MappingResult<CanonicalContent> MapToCanonical(Content source, string providerId)
    {
        try
        {
            var canonical = new CanonicalVideoContent
            {
                Id = source.Id,
                Title = source.Title,
                PublishedAt = DateTimeOffset.Parse(source.PublishedAt),
                Categories = source.Tags,
                SourceProvider = providerId,
                TransformedAt = DateTimeOffset.UtcNow,
                Metrics = new VideoMetrics
                {
                    Views = source.Metrics.Views,
                    Likes = source.Metrics.Likes,
                    Duration = ParseDuration(source.Metrics.Duration)
                }
            };

            // Validate
            var validationResult = _validator.Validate(canonical);
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
