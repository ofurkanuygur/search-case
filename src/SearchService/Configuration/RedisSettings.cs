namespace SearchService.Configuration;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string KeyPrefix { get; set; } = "searchcase";
    public int DefaultDatabase { get; set; } = 0;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
}
