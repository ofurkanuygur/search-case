namespace EventBusContracts;

/// <summary>
/// Shared event contract for content batch updates
/// Single Source of Truth - used by both EventBusService and SearchWorker
/// </summary>
public class ContentBatchUpdatedEvent
{
    public List<string> ContentIds { get; set; } = new();
    public string ChangeType { get; set; } = "Updated";
    public string? SourceProvider { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid BatchId { get; set; } = Guid.NewGuid();
    public Dictionary<string, object>? Metadata { get; set; }
}
