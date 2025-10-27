using System.Text.Json.Serialization;

namespace JsonProviderMicroservice.Models;

/// <summary>
/// Root response object from the external provider API
/// </summary>
public sealed class ContentResponse
{
    /// <summary>
    /// Collection of content items
    /// </summary>
    [JsonPropertyName("contents")]
    public List<Content> Contents { get; set; } = new();

    /// <summary>
    /// Pagination metadata
    /// </summary>
    [JsonPropertyName("pagination")]
    public Pagination Pagination { get; set; } = new();
}
