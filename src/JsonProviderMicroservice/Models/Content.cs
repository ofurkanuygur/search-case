using System.Text.Json.Serialization;

namespace JsonProviderMicroservice.Models;

/// <summary>
/// Represents a content item from the provider
/// </summary>
public sealed class Content
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Content title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content type (e.g., "video")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Performance metrics
    /// </summary>
    [JsonPropertyName("metrics")]
    public Metrics Metrics { get; set; } = new();

    /// <summary>
    /// Publication timestamp
    /// </summary>
    [JsonPropertyName("published_at")]
    public string PublishedAt { get; set; } = string.Empty;

    /// <summary>
    /// Content tags
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}
