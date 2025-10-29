namespace SearchService.Configuration;

public sealed class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "contents";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RequestTimeout { get; set; } = 60;
}
