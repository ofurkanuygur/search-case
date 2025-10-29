using WriteService.Domain.Entities;
using WriteService.Domain.Models;

namespace WriteService.Application.Interfaces;

/// <summary>
/// Interface for change detection strategies (Strategy Pattern)
/// </summary>
public interface IChangeDetectionStrategy
{
    /// <summary>
    /// Name of the strategy for logging
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Detects changes between new and existing content
    /// </summary>
    /// <param name="newContent">The new content from provider</param>
    /// <param name="existingContent">The existing content in database (if any)</param>
    /// <returns>Change detection result</returns>
    ChangeDetectionResult DetectChanges(
        ContentEntity newContent,
        ContentEntity? existingContent);

    /// <summary>
    /// Detects changes for multiple items
    /// </summary>
    /// <param name="newContents">New content items from provider</param>
    /// <param name="existingContents">Existing content items from database</param>
    /// <returns>List of change detection results</returns>
    Task<List<ChangeDetectionResult>> DetectChangesAsync(
        IEnumerable<ContentEntity> newContents,
        IEnumerable<ContentEntity> existingContents,
        CancellationToken cancellationToken = default);
}