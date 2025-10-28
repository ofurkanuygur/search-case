namespace WriteService.Domain.Enums;

/// <summary>
/// Represents the type of content (Video or Article)
/// Maps to PostgreSQL ENUM content_type
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Video content with metrics: Views, Likes, Duration
    /// </summary>
    Video = 0,

    /// <summary>
    /// Article content with metrics: ReadingTime, Reactions, Comments
    /// </summary>
    Article = 1
}
