using System.Text.Json.Serialization;

namespace JsonProviderMicroservice.Models;

/// <summary>
/// Represents performance metrics for content
/// </summary>
public sealed class Metrics
{
    /// <summary>
    /// Number of views
    /// </summary>
    [JsonPropertyName("views")]
    public int Views { get; set; }

    /// <summary>
    /// Number of likes
    /// </summary>
    [JsonPropertyName("likes")]
    public int Likes { get; set; }

    /// <summary>
    /// Duration in format MM:SS
    /// </summary>
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;
}
