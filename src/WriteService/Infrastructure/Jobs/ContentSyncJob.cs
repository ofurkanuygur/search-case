using Microsoft.Extensions.Logging;
using SearchCase.Contracts.Models;
using System.Text.Json;
using WriteService.Application.Services;
using WriteService.Application.Interfaces;
using WriteService.Data.Repositories;
using WriteService.Domain.Entities;
using WriteService.Domain.ValueObjects;
using DomainContentType = WriteService.Domain.Enums.ContentType;

namespace WriteService.Infrastructure.Jobs;

/// <summary>
/// Hangfire job that syncs content from providers to database
/// Uses ContentSyncOrchestrator for better separation of concerns
/// </summary>
public sealed class ContentSyncJob
{
    private readonly IProviderClient _providerClient;
    private readonly IScoreCalculationService _scoreCalculationService;
    private readonly ContentSyncOrchestrator _syncOrchestrator;
    private readonly ILogger<ContentSyncJob> _logger;

    public ContentSyncJob(
        IProviderClient providerClient,
        IScoreCalculationService scoreCalculationService,
        ContentSyncOrchestrator syncOrchestrator,
        ILogger<ContentSyncJob> logger)
    {
        _providerClient = providerClient;
        _scoreCalculationService = scoreCalculationService;
        _syncOrchestrator = syncOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Executes the content sync pipeline
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Content Sync Job");

        try
        {
            // 1. Fetch from all providers
            var providerResponse = await _providerClient.FetchFromAllProvidersAsync(cancellationToken);

            if (providerResponse.Items.Count == 0)
            {
                _logger.LogWarning("No items fetched from providers");
                return;
            }

            _logger.LogInformation(
                "Fetched {Count} items from providers",
                providerResponse.Items.Count);

            // 2. Convert to domain entities (without score calculation)
            var entities = providerResponse.Items
                .Select(MapToEntity)
                .ToList();

            // Create a mapping for score calculation later (only for changed items)
            var canonicalMapping = providerResponse.Items
                .ToDictionary(c => $"{c.SourceProvider}_{c.Id}", c => c);

            // 3. Use orchestrator for synchronized operations with score calculation
            var syncResult = await _syncOrchestrator.SynchronizeContentAsync(
                entities,
                canonicalMapping,
                _scoreCalculationService,
                cancellationToken);

            _logger.LogInformation(
                "Content Sync Job completed: {Summary}",
                syncResult.GetSummary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content Sync Job failed");
            throw;
        }
    }

    private ContentEntity MapToEntity(CanonicalContent canonical)
    {
        // Compute hash FIRST (for change detection)
        var hash = ContentHash.ComputeFrom(canonical);

        // Score will be calculated later, only for changed items
        // For now, use a default score (will be updated if changed)
        var score = Score.Create(0);

        // Serialize metrics to JsonElement
        var metricsJson = canonical switch
        {
            CanonicalVideoContent video => JsonSerializer.SerializeToElement(video.Metrics),
            CanonicalArticleContent article => JsonSerializer.SerializeToElement(article.Metrics),
            _ => throw new NotSupportedException()
        };

        // Map type
        var type = canonical switch
        {
            CanonicalVideoContent => DomainContentType.Video,
            CanonicalArticleContent => DomainContentType.Article,
            _ => throw new NotSupportedException()
        };

        // Create unique ID by combining source provider and original ID
        // This prevents ID conflicts between different providers
        var uniqueId = $"{canonical.SourceProvider}_{canonical.Id}";

        return ContentEntity.Create(
            id: uniqueId,
            type: type,
            title: canonical.Title,
            publishedAt: canonical.PublishedAt,
            categories: canonical.Categories,
            sourceProvider: canonical.SourceProvider,
            metrics: metricsJson,
            score: score,
            contentHash: hash);
    }
}
