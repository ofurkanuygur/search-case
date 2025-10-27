using System.Text.Json.Serialization;

namespace JsonProviderMicroservice.Models;

/// <summary>
/// Represents pagination metadata
/// </summary>
public sealed class Pagination
{
    /// <summary>
    /// Total number of items available
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }
}
