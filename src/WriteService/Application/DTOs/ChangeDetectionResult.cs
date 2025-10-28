using SearchCase.Contracts.Models;

namespace WriteService.Application.DTOs;

/// <summary>
/// Result of change detection operation
/// Categorizes content into NEW, UPDATED, and UNCHANGED
/// </summary>
public sealed class ChangeDetectionResult
{
    /// <summary>
    /// Content items that are completely new (not in DB)
    /// </summary>
    public List<CanonicalContent> NewItems { get; init; } = new();

    /// <summary>
    /// Content items that exist but have changed (hash mismatch)
    /// </summary>
    public List<CanonicalContent> UpdatedItems { get; init; } = new();

    /// <summary>
    /// Content items that haven't changed (hash match)
    /// These will be skipped in processing
    /// </summary>
    public List<CanonicalContent> UnchangedItems { get; init; } = new();

    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int TotalProcessed => NewItems.Count + UpdatedItems.Count + UnchangedItems.Count;

    /// <summary>
    /// Number of items that need processing (NEW + UPDATED)
    /// </summary>
    public int ItemsToProcess => NewItems.Count + UpdatedItems.Count;

    /// <summary>
    /// Percentage of changed items
    /// </summary>
    public decimal ChangePercentage =>
        TotalProcessed == 0 ? 0m : Math.Round((ItemsToProcess / (decimal)TotalProcessed) * 100, 2);
}
