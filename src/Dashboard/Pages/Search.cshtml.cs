using Dashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchCase.Search.Contracts.Enums;
using SearchCase.Search.Contracts.Models;

namespace Dashboard.Pages;

public class SearchModel : PageModel
{
    private readonly ISearchServiceClient _searchServiceClient;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(ISearchServiceClient searchServiceClient, ILogger<SearchModel> logger)
    {
        _searchServiceClient = searchServiceClient ?? throw new ArgumentNullException(nameof(searchServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Bind query parameters
    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; } = "Score";

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public double? MinScore { get; set; }

    [BindProperty(SupportsGet = true)]
    public double? MaxScore { get; set; }

    // Results
    public SearchResult? SearchResult { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Search requested - Keyword: {Keyword}, Type: {Type}, Sort: {Sort}, Page: {Page}",
                Keyword, Type, Sort, CurrentPage);

            SearchResult = await _searchServiceClient.SearchAsync(
                keyword: Keyword,
                type: Type,
                sort: Sort,
                page: CurrentPage,
                pageSize: PageSize,
                minScore: MinScore,
                maxScore: MaxScore,
                cancellationToken: cancellationToken);

            if (SearchResult == null)
            {
                HasError = true;
                ErrorMessage = "SearchService is unavailable. Please ensure the service is running.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            HasError = true;
            ErrorMessage = "An error occurred while searching. Please try again.";
        }
    }

    // Helper methods for UI
    public string GetContentTypeBadgeClass(ContentType type)
    {
        return type switch
        {
            ContentType.Video => "badge bg-danger",
            ContentType.Article => "badge bg-primary",
            _ => "badge bg-secondary"
        };
    }

    public string GetContentTypeIcon(ContentType type)
    {
        return type switch
        {
            ContentType.Video => "🎥",
            ContentType.Article => "📄",
            _ => "📋"
        };
    }

    public int GetTotalPages()
    {
        if (SearchResult?.Pagination == null) return 1;

        var totalPages = (int)Math.Ceiling(
            (double)SearchResult.Pagination.TotalItems / SearchResult.Pagination.PageSize);

        return Math.Max(1, totalPages);
    }

    public List<int> GetPageNumbers()
    {
        var totalPages = GetTotalPages();
        var currentPage = SearchResult?.Pagination?.CurrentPage ?? 1;

        // Show max 10 page numbers
        var maxPages = 10;
        var halfMax = maxPages / 2;

        var start = Math.Max(1, currentPage - halfMax);
        var end = Math.Min(totalPages, start + maxPages - 1);

        // Adjust start if we're near the end
        if (end - start < maxPages - 1)
        {
            start = Math.Max(1, end - maxPages + 1);
        }

        return Enumerable.Range(start, end - start + 1).ToList();
    }
}
