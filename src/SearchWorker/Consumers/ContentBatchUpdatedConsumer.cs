using MassTransit;
using SearchWorker.Data.Repositories;
using EventBusContracts;
using SearchWorker.Services;

namespace SearchWorker.Consumers;

/// <summary>
/// Consumes ContentBatchUpdatedEvent from RabbitMQ
/// Implements "Single Source of Truth" pattern - fetches from DB and indexes to Elasticsearch
/// Follows Single Responsibility Principle - only handles event consumption
/// </summary>
public class ContentBatchUpdatedConsumer : IConsumer<ContentBatchUpdatedEvent>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<ContentBatchUpdatedConsumer> _logger;

    public ContentBatchUpdatedConsumer(
        IElasticsearchService elasticsearchService,
        ISearchRepository searchRepository,
        ILogger<ContentBatchUpdatedConsumer> logger)
    {
        _elasticsearchService = elasticsearchService ?? throw new ArgumentNullException(nameof(elasticsearchService));
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
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
            var documents = await _searchRepository.GetByIdsAsync(message.ContentIds, context.CancellationToken);

            if (!documents.Any())
            {
                _logger.LogWarning(
                    "No documents found in database for IDs. BatchId={BatchId}, RequestedCount={Count}",
                    message.BatchId,
                    message.ContentIds.Count);
                return;
            }

            _logger.LogInformation(
                "Fetched {Count} documents from database (pre-calculated scores included)",
                documents.Count);

            // Step 2: Index documents to Elasticsearch
            var indexResult = await _elasticsearchService.IndexDocumentsAsync(documents, context.CancellationToken);

            // Step 3: Log results
            _logger.LogInformation(
                "Index update completed: BatchId={BatchId}, Total={Total}, Success={Success}, Failed={Failed}, Duration={Duration}ms",
                message.BatchId,
                indexResult.TotalProcessed,
                indexResult.SuccessCount,
                indexResult.FailedCount,
                indexResult.Duration.TotalMilliseconds);

            if (indexResult.FailedIds.Any())
            {
                _logger.LogWarning(
                    "Failed to index documents: {FailedIds}",
                    string.Join(", ", indexResult.FailedIds));
            }
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
