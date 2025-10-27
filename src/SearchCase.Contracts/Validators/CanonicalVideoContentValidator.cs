using FluentValidation;
using SearchCase.Contracts.Models;

namespace SearchCase.Contracts.Validators;

/// <summary>
/// Validator for canonical video content
/// </summary>
public sealed class CanonicalVideoContentValidator : AbstractValidator<CanonicalVideoContent>
{
    public CanonicalVideoContentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Video ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Video title is required")
            .MaximumLength(500)
            .WithMessage("Video title must not exceed 500 characters");

        RuleFor(x => x.PublishedAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Published date cannot be in the future");

        RuleFor(x => x.Categories)
            .NotEmpty()
            .WithMessage("At least one category is required");

        RuleFor(x => x.SourceProvider)
            .NotEmpty()
            .WithMessage("Source provider is required");

        RuleFor(x => x.Metrics.Views)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Views must be non-negative");

        RuleFor(x => x.Metrics.Likes)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Likes must be non-negative");

        RuleFor(x => x.Metrics.Duration)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Duration must be greater than zero");
    }
}
