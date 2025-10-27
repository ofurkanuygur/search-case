namespace SearchCase.Contracts.Responses;

/// <summary>
/// Metadata about the provider and fetch operation
/// </summary>
public sealed class ProviderMetadata
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTimeOffset FetchedAt { get; set; }
    public TimeSpan FetchDuration { get; set; }
}
