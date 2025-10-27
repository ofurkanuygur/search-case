using FluentValidation;
using SearchCase.Contracts.Models;

namespace SearchCase.Contracts.Validators;

/// <summary>
/// Validator for canonical article content
/// </summary>
public sealed class CanonicalArticleContentValidator : AbstractValidator<CanonicalArticleContent>
{
    public CanonicalArticleContentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Article ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Article title is required")
            .MaximumLength(500)
            .WithMessage("Article title must not exceed 500 characters");

        RuleFor(x => x.PublishedAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Published date cannot be in the future");

        RuleFor(x => x.Categories)
            .NotEmpty()
            .WithMessage("At least one category is required");

        RuleFor(x => x.SourceProvider)
            .NotEmpty()
            .WithMessage("Source provider is required");

        RuleFor(x => x.Metrics.ReadingTimeMinutes)
            .GreaterThan(0)
            .WithMessage("Reading time must be greater than zero");

        RuleFor(x => x.Metrics.Reactions)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Reactions must be non-negative");

        RuleFor(x => x.Metrics.Comments)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Comments must be non-negative");
    }
}
