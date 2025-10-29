using FluentValidation;
using SearchCase.Search.Contracts.Models;

namespace SearchCase.Search.Contracts.Validators;

/// <summary>
/// Validator for SearchRequest
/// </summary>
public sealed class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    private const int MaxPageSize = 100;
    private const int MinPageSize = 1;
    private const int MaxPage = 10000;

    public SearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0")
            .LessThanOrEqualTo(MaxPage)
            .WithMessage($"Page must not exceed {MaxPage}");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(MinPageSize)
            .WithMessage($"PageSize must be at least {MinPageSize}")
            .LessThanOrEqualTo(MaxPageSize)
            .WithMessage($"PageSize must not exceed {MaxPageSize}");

        RuleFor(x => x.Keyword)
            .MaximumLength(200)
            .WithMessage("Keyword must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Keyword));

        RuleFor(x => x.MinScore)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinScore must be greater than or equal to 0")
            .When(x => x.MinScore.HasValue);

        RuleFor(x => x.MaxScore)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxScore must be greater than or equal to 0")
            .When(x => x.MaxScore.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinScore.HasValue || !x.MaxScore.HasValue || x.MinScore <= x.MaxScore)
            .WithMessage("MinScore must be less than or equal to MaxScore")
            .When(x => x.MinScore.HasValue && x.MaxScore.HasValue);
    }
}
