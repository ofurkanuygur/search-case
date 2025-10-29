namespace CacheWorker.Events;

/// <summary>
/// Event received from EventBus with optimized payload (only IDs)
/// Following "Single Source of Truth" pattern
/// </summary>
public class ContentBatchUpdatedEvent
{
    public List<string> ContentIds { get; set; } = new();
    public string ChangeType { get; set; } = "Updated";
    public string? SourceProvider { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
    public Guid BatchId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}