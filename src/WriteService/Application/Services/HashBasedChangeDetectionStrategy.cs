using System.Text.Json;
using Microsoft.Extensions.Logging;
using WriteService.Domain.Entities;
using WriteService.Domain.Enums;
using WriteService.Domain.Models;
using WriteService.Application.Interfaces;

namespace WriteService.Application.Services;

/// <summary>
/// Change detection strategy based on content hash comparison
/// </summary>
public sealed class HashBasedChangeDetectionStrategy : IChangeDetectionStrategy
{
    private readonly ILogger<HashBasedChangeDetectionStrategy> _logger;

    public string Name => "HashBased";

    public HashBasedChangeDetectionStrategy(ILogger<HashBasedChangeDetectionStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ChangeDetectionResult DetectChanges(
        ContentEntity newContent,
        ContentEntity? existingContent)
    {
        if (newContent == null)
            throw new ArgumentNullException(nameof(newContent));

        // New content
        if (existingContent == null)
        {
            _logger.LogDebug("Content {ContentId} is new", newContent.Id);
            return ChangeDetectionResult.NewContent(newContent);
        }

        // Compare hashes
        if (newContent.ContentHash.Equals(existingContent.ContentHash))
        {
            _logger.LogDebug("Content {ContentId} is unchanged (hash match)", newContent.Id);
            return ChangeDetectionResult.Unchanged(newContent, existingContent);
        }

        // Detect which fields changed
        var changedFields = DetectChangedFields(newContent, existingContent);

        _logger.LogDebug(
            "Content {ContentId} has {FieldCount} changed fields",
            newContent.Id,
            changedFields.Count);

        return ChangeDetectionResult.Updated(newContent, existingContent, changedFields);
    }

    public async Task<List<ChangeDetectionResult>> DetectChangesAsync(
        IEnumerable<ContentEntity> newContents,
        IEnumerable<ContentEntity> existingContents,
        CancellationToken cancellationToken = default)
    {
        var newContentsList = newContents?.ToList() ?? new List<ContentEntity>();
        var existingContentsDict = existingContents?.ToDictionary(c => c.Id) ?? new Dictionary<string, ContentEntity>();

        var results = new List<ChangeDetectionResult>();

        _logger.LogInformation(
            "Detecting changes for {NewCount} items against {ExistingCount} existing items",
            newContentsList.Count,
            existingContentsDict.Count);

        foreach (var newContent in newContentsList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            existingContentsDict.TryGetValue(newContent.Id, out var existingContent);
            var result = DetectChanges(newContent, existingContent);
            results.Add(result);
        }

        // Log summary
        var created = results.Count(r => r.ChangeType == ChangeType.Created);
        var updated = results.Count(r => r.ChangeType == ChangeType.Updated);
        var unchanged = results.Count(r => r.ChangeType == ChangeType.None);

        _logger.LogInformation(
            "Change detection complete: {Created} created, {Updated} updated, {Unchanged} unchanged",
            created,
            updated,
            unchanged);

        return await Task.FromResult(results);
    }

    private List<ChangedField> DetectChangedFields(ContentEntity newContent, ContentEntity existingContent)
    {
        var changes = new List<ChangedField>();

        // Title
        if (newContent.Title != existingContent.Title)
        {
            changes.Add(new ChangedField(
                "Title",
                existingContent.Title,
                newContent.Title,
                isSignificant: true));
        }

        // Published date
        if (newContent.PublishedAt != existingContent.PublishedAt)
        {
            changes.Add(new ChangedField(
                "PublishedAt",
                existingContent.PublishedAt.ToString("O"),
                newContent.PublishedAt.ToString("O"),
                isSignificant: false));
        }

        // Categories
        var oldCategories = JsonSerializer.Serialize(existingContent.Categories);
        var newCategories = JsonSerializer.Serialize(newContent.Categories);
        if (oldCategories != newCategories)
        {
            changes.Add(new ChangedField(
                "Categories",
                oldCategories,
                newCategories,
                isSignificant: true));
        }

        // Metrics
        var oldMetrics = JsonSerializer.Serialize(existingContent.Metrics);
        var newMetrics = JsonSerializer.Serialize(newContent.Metrics);
        if (oldMetrics != newMetrics)
        {
            changes.Add(new ChangedField(
                "Metrics",
                oldMetrics,
                newMetrics,
                isSignificant: true));
        }

        // Score
        if (Math.Abs(newContent.Score.Value - existingContent.Score.Value) > 0.01m)
        {
            changes.Add(new ChangedField(
                "Score",
                existingContent.Score.Value.ToString("F2"),
                newContent.Score.Value.ToString("F2"),
                isSignificant: Math.Abs(newContent.Score.Value - existingContent.Score.Value) > 10));
        }

        // Source provider
        if (newContent.SourceProvider != existingContent.SourceProvider)
        {
            changes.Add(new ChangedField(
                "SourceProvider",
                existingContent.SourceProvider,
                newContent.SourceProvider,
                isSignificant: false));
        }

        return changes;
    }
}