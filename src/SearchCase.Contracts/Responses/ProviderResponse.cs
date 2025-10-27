using SearchCase.Contracts.Models;

namespace SearchCase.Contracts.Responses;

/// <summary>
/// Standard response wrapper for provider data
/// </summary>
public sealed class ProviderResponse
{
    /// <summary>
    /// List of canonical content items
    /// </summary>
    public List<CanonicalContent> Items { get; set; } = new();

    /// <summary>
    /// Pagination information
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();

    /// <summary>
    /// Provider source metadata
    /// </summary>
    public ProviderMetadata Provider { get; set; } = new();

    /// <summary>
    /// Any errors or warnings during transformation
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
