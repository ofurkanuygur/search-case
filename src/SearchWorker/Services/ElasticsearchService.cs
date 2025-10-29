using System.Diagnostics;
using Microsoft.Extensions.Options;
using Nest;
using SearchWorker.Configuration;
using SearchWorker.Models;

namespace SearchWorker.Services;

/// <summary>
/// Elasticsearch service implementation using NEST library
/// Follows Single Responsibility Principle - only handles Elasticsearch operations
/// </summary>
public sealed class ElasticsearchService : IElasticsearchService, IDisposable
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly ElasticsearchOptions _options;
    private readonly string _indexName;

    public ElasticsearchService(
        IElasticClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _indexName = _options.IndexName;

        _logger.LogInformation("Elasticsearch service initialized with index: {IndexName}", _indexName);
    }

    public async Task<bool> IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            var response = await _client.IndexAsync(document, idx => idx
                .Index(_indexName)
                .Id(document.Id),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to index document {Id}: {Error}",
                    document.Id,
                    response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully indexed document {Id}", document.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while indexing document {Id}", document.Id);
            return false;
        }
    }

    public async Task<IndexResult> IndexDocumentsAsync(List<SearchDocument> documents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documents);

        if (!documents.Any())
        {
            return IndexResult.Success(0, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();
        var failedIds = new List<string>();

        try
        {
            // Use bulk indexing for better performance
            var bulkDescriptor = new BulkDescriptor();

            foreach (var document in documents)
            {
                bulkDescriptor.Index<SearchDocument>(op => op
                    .Index(_indexName)
                    .Id(document.Id)
                    .Document(document));
            }

            var response = await _client.BulkAsync(bulkDescriptor, cancellationToken);
            stopwatch.Stop();

            if (!response.IsValid)
            {
                _logger.LogError("Bulk indexing failed: {Error}", response.DebugInformation);
                return IndexResult.Failure(
                    documents.Count,
                    documents.Select(d => d.Id).ToList(),
                    stopwatch.Elapsed,
                    response.DebugInformation);
            }

            // Check individual item results
            var successCount = 0;
            foreach (var item in response.Items)
            {
                if (item.IsValid)
                {
                    successCount++;
                }
                else
                {
                    failedIds.Add(item.Id);
                    _logger.LogWarning("Failed to index document {Id}: {Error}",
                        item.Id,
                        item.Error?.Reason);
                }
            }

            if (failedIds.Any())
            {
                _logger.LogWarning(
                    "Partial bulk indexing: {Success}/{Total} succeeded, {Failed} failed",
                    successCount,
                    documents.Count,
                    failedIds.Count);

                return IndexResult.PartialSuccess(documents.Count, successCount, failedIds, stopwatch.Elapsed);
            }

            _logger.LogInformation(
                "Successfully indexed {Count} documents in {Duration}ms",
                successCount,
                stopwatch.ElapsedMilliseconds);

            return IndexResult.Success(successCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk indexing");
            stopwatch.Stop();
            return IndexResult.Failure(
                documents.Count,
                documents.Select(d => d.Id).ToList(),
                stopwatch.Elapsed,
                ex.Message);
        }
    }

    public async Task<SearchResult> SearchAsync(string query, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var from = (page - 1) * pageSize;

            var response = await _client.SearchAsync<SearchDocument>(s => s
                .Index(_indexName)
                .From(from)
                .Size(pageSize)
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query)
                        .Fields(f => f
                            .Field(doc => doc.Title, boost: 2.0)
                            .Field(doc => doc.SearchableText)
                            .Field(doc => doc.Description)
                            .Field(doc => doc.Categories))
                        .Type(TextQueryType.BestFields)
                        .Fuzziness(Fuzziness.Auto)))
                .Sort(sort => sort
                    .Descending(SortSpecialField.Score)
                    .Descending(doc => doc.Score))
                .TrackTotalHits(true),
                cancellationToken);

            stopwatch.Stop();

            if (!response.IsValid)
            {
                _logger.LogError("Search failed: {Error}", response.DebugInformation);
                return new SearchResult
                {
                    Documents = new List<SearchDocument>(),
                    TotalHits = 0,
                    Page = page,
                    PageSize = pageSize,
                    QueryTime = stopwatch.Elapsed
                };
            }

            _logger.LogInformation(
                "Search completed: query='{Query}', hits={Hits}, time={Time}ms",
                query,
                response.Total,
                stopwatch.ElapsedMilliseconds);

            return new SearchResult
            {
                Documents = response.Documents.ToList(),
                TotalHits = response.Total,
                Page = page,
                PageSize = pageSize,
                QueryTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during search for query: {Query}", query);
            stopwatch.Stop();
            return new SearchResult
            {
                Documents = new List<SearchDocument>(),
                TotalHits = 0,
                Page = page,
                PageSize = pageSize,
                QueryTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<SearchDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var response = await _client.GetAsync<SearchDocument>(id, g => g.Index(_indexName), cancellationToken);

            if (!response.IsValid || !response.Found)
            {
                _logger.LogDebug("Document {Id} not found in index", id);
                return null;
            }

            return response.Source;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting document {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var response = await _client.DeleteAsync<SearchDocument>(id, d => d.Index(_indexName), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogWarning("Failed to delete document {Id}: {Error}",
                    id,
                    response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully deleted document {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting document {Id}", id);
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync(_indexName, ct: cancellationToken);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking index existence");
            return false;
        }
    }

    public async Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await IndexExistsAsync(cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Index {IndexName} already exists", _indexName);
                return true;
            }

            var response = await _client.Indices.CreateAsync(_indexName, c => c
                .Map<SearchDocument>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.Title)
                            .Analyzer("standard")
                            .Fields(f => f
                                .Keyword(k => k.Name("keyword"))))
                        .Text(t => t
                            .Name(n => n.SearchableText)
                            .Analyzer("standard"))
                        .Text(t => t
                            .Name(n => n.Description)
                            .Analyzer("standard"))
                        .Keyword(k => k
                            .Name(n => n.ContentType))
                        .Keyword(k => k
                            .Name(n => n.SourceProvider))
                        .Number(n => n
                            .Name(d => d.Score)
                            .Type(NumberType.Double))
                        .Date(d => d
                            .Name(n => n.PublishedAt))
                        .Date(d => d
                            .Name(n => n.IndexedAt))
                        .Keyword(k => k
                            .Name(n => n.Categories))))
                .Settings(s => s
                    .NumberOfShards(_options.NumberOfShards)
                    .NumberOfReplicas(_options.NumberOfReplicas)
                    .RefreshInterval(TimeSpan.FromSeconds(_options.RefreshIntervalSeconds))),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}",
                    _indexName,
                    response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully created index {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating index {IndexName}", _indexName);
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.DeleteAsync(_indexName, ct: cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}",
                    _indexName,
                    response.DebugInformation);
                return false;
            }

            _logger.LogWarning("Successfully deleted index {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting index {IndexName}", _indexName);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = new Dictionary<string, object>();

            var statsResponse = await _client.Indices.StatsAsync(_indexName, ct: cancellationToken);
            if (statsResponse.IsValid)
            {
                var indexStats = statsResponse.Indices[_indexName];
                stats["document_count"] = indexStats.Total.Documents.Count;
                stats["store_size_bytes"] = indexStats.Total.Store.SizeInBytes;
                stats["index_name"] = _indexName;
            }

            var healthResponse = await _client.Cluster.HealthAsync(ct: cancellationToken);
            if (healthResponse.IsValid)
            {
                stats["status"] = healthResponse.Status.ToString();
                stats["number_of_shards"] = healthResponse.NumberOfDataNodes;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting index stats");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message
            };
        }
    }

    public void Dispose()
    {
        // NEST client doesn't need explicit disposal
    }
}
