using CacheWorker.Services;
using EventBusContracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CacheWorker.Consumers;

/// <summary>
/// Consumes ContentBatchUpdatedEvent from RabbitMQ
/// Implements "Single Source of Truth" pattern - fetches from DB with pre-calculated scores
/// NO score recalculation happens here
/// </summary>
public class ContentBatchUpdatedConsumer : IConsumer<ContentBatchUpdatedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly IContentRepository _contentRepository;
    private readonly ILogger<ContentBatchUpdatedConsumer> _logger;

    public ContentBatchUpdatedConsumer(
        ICacheService cacheService,
        IContentRepository contentRepository,
        ILogger<ContentBatchUpdatedConsumer> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ContentBatchUpdatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Received batch update event: BatchId={BatchId}, ContentCount={Count}, ChangeType={ChangeType}",
            message.BatchId,
            message.ContentIds.Count,
            message.ChangeType);

        try
        {
            if (!message.ContentIds.Any())
            {
                _logger.LogWarning("Received empty content batch");
                return;
            }

            // Step 1: Fetch full content from database (with pre-calculated scores)
            // This is the "Single Source of Truth" - we trust the database
            var contents = await _contentRepository.GetByIdsAsync(message.ContentIds, context.CancellationToken);

            if (!contents.Any())
            {
                _logger.LogWarning(
                    "No contents found in database for IDs. BatchId={BatchId}, RequestedCount={Count}",
                    message.BatchId,
                    message.ContentIds.Count);
                return;
            }

            _logger.LogInformation(
                "Fetched {Count} contents from database (pre-calculated scores included)",
                contents.Count);

            // Step 2: Update Redis cache with fetched content
            // NO score calculation here - we use the scores from the database
            var cacheResults = await _cacheService.UpdateCacheAsync(contents, context.CancellationToken);

            // Step 3: Log results
            _logger.LogInformation(
                "Cache update completed: BatchId={BatchId}, Total={Total}, Success={Success}, Failed={Failed}",
                message.BatchId,
                cacheResults.TotalProcessed,
                cacheResults.SuccessCount,
                cacheResults.FailedCount);

            // Update cache statistics
            await _cacheService.UpdateStatisticsAsync(
                message.ChangeType,
                cacheResults.SuccessCount,
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process content batch update. BatchId={BatchId}, ContentCount={Count}",
                message.BatchId,
                message.ContentIds.Count);

            // Re-throw to let MassTransit handle retry
            throw;
        }
    }
}