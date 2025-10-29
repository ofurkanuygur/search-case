namespace WriteService.Domain.Enums;

/// <summary>
/// Represents the type of change detected for content
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// No change detected
    /// </summary>
    None = 0,

    /// <summary>
    /// New content that doesn't exist in database
    /// </summary>
    Created = 1,

    /// <summary>
    /// Existing content with modified data
    /// </summary>
    Updated = 2,

    /// <summary>
    /// Content that was previously deleted and now restored
    /// </summary>
    Restored = 3,

    /// <summary>
    /// Content that exists in database but not in source
    /// </summary>
    Deleted = 4
}