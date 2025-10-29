using Microsoft.Extensions.Logging;
using SearchCase.Contracts.Models;
using WriteService.Application.DTOs;
using WriteService.Data.Repositories;
using WriteService.Domain.ValueObjects;

namespace WriteService.Application.Services;

/// <summary>
/// Implementation of change detection using SHA256 hash comparison
/// </summary>
public sealed class ChangeDetectionService : IChangeDetectionService
{
    private readonly IContentRepository _repository;
    private readonly ILogger<ChangeDetectionService> _logger;

    public ChangeDetectionService(
        IContentRepository repository,
        ILogger<ChangeDetectionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChangeDetectionResult> DetectChangesAsync(
        List<CanonicalContent> contents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting change detection for {Count} items", contents.Count);

        var result = new ChangeDetectionResult();

        // Compute hashes for incoming content
        var contentHashes = contents
            .Select(c => new
            {
                Content = c,
                Hash = ContentHash.ComputeFrom(c)
            })
            .ToList();

        // Get existing content IDs
        var contentIds = contents.Select(c => c.Id).ToList();
        var existingContents = await _repository.GetByIdsAsync(contentIds, cancellationToken);
        var existingDict = existingContents.ToDictionary(c => c.Id, c => c);

        // Categorize each item
        foreach (var item in contentHashes)
        {
            if (!existingDict.TryGetValue(item.Content.Id, out var existing))
            {
                // NEW: Content doesn't exist in DB
                result.NewItems.Add(item.Content);
            }
            else if (existing.HasChanged(item.Hash))
            {
                // UPDATED: Content exists but hash is different
                result.UpdatedItems.Add(item.Content);
            }
            else
            {
                // UNCHANGED: Content exists and hash matches
                result.UnchangedItems.Add(item.Content);
            }
        }

        _logger.LogInformation(
            "Change detection complete: NEW={New}, UPDATED={Updated}, UNCHANGED={Unchanged}, ChangeRate={ChangeRate}%",
            result.NewItems.Count,
            result.UpdatedItems.Count,
            result.UnchangedItems.Count,
            result.ChangePercentage);

        return result;
    }
}
