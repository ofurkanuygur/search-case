using System.Text.Json.Serialization;

namespace SearchService.Clients.Elasticsearch;

/// <summary>
/// Elasticsearch document model for content
/// Note: Field mapping is handled by SearchWorker during indexing
/// </summary>
public sealed class ContentDocument
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;

    [JsonPropertyName("contentType")]
    public string Type { get; set; } = default!;

    public double Score { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public List<string> Categories { get; set; } = new();
    public string SourceProvider { get; set; } = default!;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
