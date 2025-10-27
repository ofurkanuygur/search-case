using System.Text.Json.Serialization;

namespace SearchCase.Contracts.Models;

/// <summary>
/// Base class for all canonical content types.
/// Uses discriminated union pattern with JSON polymorphism.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CanonicalVideoContent), "video")]
[JsonDerivedType(typeof(CanonicalArticleContent), "article")]
public abstract class CanonicalContent
{
    /// <summary>
    /// Unique identifier from the provider
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Content title/headline
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content type (discriminator for derived types)
    /// </summary>
    public abstract ContentType Type { get; }

    /// <summary>
    /// Publication date and time
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// Content categories/tags
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Source provider identifier (e.g., "provider1", "provider2")
    /// </summary>
    public string SourceProvider { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this content was transformed to canonical format
    /// </summary>
    public DateTimeOffset TransformedAt { get; set; }
}
