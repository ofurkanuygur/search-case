using SearchWorker.Models;

namespace SearchWorker.Services;

/// <summary>
/// Service for managing Elasticsearch operations
/// Follows Interface Segregation Principle - focused interface for search operations
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// Index a single document
    /// </summary>
    Task<bool> IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch index multiple documents
    /// </summary>
    Task<IndexResult> IndexDocumentsAsync(List<SearchDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search documents by query
    /// </summary>
    Task<SearchResult> SearchAsync(string query, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get document by ID
    /// </summary>
    Task<SearchDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete document by ID
    /// </summary>
    Task<bool> DeleteDocumentAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if index exists
    /// </summary>
    Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create index with proper mappings
    /// </summary>
    Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete index
    /// </summary>
    Task<bool> DeleteIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get index statistics
    /// </summary>
    Task<Dictionary<string, object>> GetIndexStatsAsync(CancellationToken cancellationToken = default);
}
