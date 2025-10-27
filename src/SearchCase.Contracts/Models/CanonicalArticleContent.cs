namespace SearchCase.Contracts.Models;

/// <summary>
/// Canonical representation of article content
/// </summary>
public sealed class CanonicalArticleContent : CanonicalContent
{
    public override ContentType Type => ContentType.Article;

    public ArticleMetrics Metrics { get; set; } = new();
}
