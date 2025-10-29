using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using SearchCase.Search.Contracts.Enums;
using SearchCase.Search.Contracts.Models;
using System.Diagnostics;
using ContractsSearchRequest = SearchCase.Search.Contracts.Models.SearchRequest;
using ContractsSearchResult = SearchCase.Search.Contracts.Models.SearchResult;

namespace SearchService.Clients.Elasticsearch;

/// <summary>
/// Elasticsearch search client implementation with BM25 relevance
/// </summary>
public sealed class ElasticsearchSearchClient : IElasticsearchSearchClient
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchSearchClient> _logger;
    private readonly string _indexName;

    public ElasticsearchSearchClient(
        ElasticsearchClient client,
        ILogger<ElasticsearchSearchClient> logger,
        string indexName)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _indexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
    }

    public async Task<ContractsSearchResult> SearchAsync(
        ContractsSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Build Elasticsearch query
            var searchResponse = await _client.SearchAsync<ContentDocument>(s => s
                .Index(_indexName)
                .From((request.Page - 1) * request.PageSize)
                .Size(request.PageSize)
                .Query(BuildQuery(request))
                .Sort(BuildSort(request))
                .TrackTotalHits(new Elastic.Clients.Elasticsearch.Core.Search.TrackHits(true)),
                cancellationToken
            );

            stopwatch.Stop();

            if (!searchResponse.IsValidResponse)
            {
                _logger.LogError(
                    "Elasticsearch query failed: {Error}",
                    searchResponse.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");

                throw new InvalidOperationException(
                    $"Elasticsearch query failed: {searchResponse.ElasticsearchServerError?.Error?.Reason}");
            }

            var items = searchResponse.Documents.Select(MapToDto).ToList();
            var total = searchResponse.Total > 0 ? (int)searchResponse.Total : 0;

            _logger.LogInformation(
                "Elasticsearch search completed: {Total} results, {Latency}ms",
                total, stopwatch.ElapsedMilliseconds);

            return new ContractsSearchResult
            {
                Items = items,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = total
                },
                Metadata = new SearchMetadata
                {
                    Strategy = "Elasticsearch",
                    DataSource = "Elasticsearch",
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    FromCache = false,
                    Timestamp = DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch search failed");
            throw;
        }
    }

    private Query BuildQuery(ContractsSearchRequest request)
    {
        // Start with match_all if no keyword
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            // Simple filter by type if specified
            if (request.Type.HasValue)
            {
                return Query.Term(new TermQuery(new Field("contentType"))
                {
                    Value = request.Type.Value.ToString().ToLowerInvariant()
                });
            }

            return Query.MatchAll(new MatchAllQuery());
        }

        // Build boolean query with keyword search
        var mustClauses = new List<Query>
        {
            Query.MultiMatch(new MultiMatchQuery
            {
                Query = request.Keyword,
                Fields = new[] { new Field("title"), new Field("categories") },
                Type = TextQueryType.BestFields,
                Fuzziness = new Fuzziness("AUTO"),
                Operator = Operator.Or
            })
        };

        // Add type filter if specified
        if (request.Type.HasValue)
        {
            var filterClauses = new List<Query>
            {
                Query.Term(new TermQuery(new Field("contentType"))
                {
                    Value = request.Type.Value.ToString().ToLowerInvariant()
                })
            };

            return Query.Bool(new BoolQuery
            {
                Must = mustClauses,
                Filter = filterClauses
            });
        }

        return Query.Bool(new BoolQuery
        {
            Must = mustClauses
        });
    }

    private ICollection<SortOptions> BuildSort(ContractsSearchRequest request)
    {
        return request.Sort switch
        {
            SortBy.Score => new List<SortOptions>
            {
                SortOptions.Field(new Field("score"), new FieldSort { Order = SortOrder.Desc }),
                SortOptions.Field(new Field("publishedAt"), new FieldSort { Order = SortOrder.Desc })
            },
            SortBy.Relevance => new List<SortOptions>
            {
                SortOptions.Score(new ScoreSort { Order = SortOrder.Desc }),
                SortOptions.Field(new Field("score"), new FieldSort { Order = SortOrder.Desc })
            },
            SortBy.Date => new List<SortOptions>
            {
                SortOptions.Field(new Field("publishedAt"), new FieldSort { Order = SortOrder.Desc }),
                SortOptions.Field(new Field("score"), new FieldSort { Order = SortOrder.Desc })
            },
            _ => new List<SortOptions>
            {
                SortOptions.Field(new Field("score"), new FieldSort { Order = SortOrder.Desc })
            }
        };
    }

    private ContentDto MapToDto(ContentDocument doc)
    {
        return new ContentDto
        {
            Id = doc.Id,
            Title = doc.Title,
            Type = Enum.Parse<ContentType>(doc.Type, ignoreCase: true),
            Score = doc.Score,
            PublishedAt = doc.PublishedAt,
            Categories = doc.Categories,
            SourceProvider = doc.SourceProvider,
            Metrics = doc.Metadata
        };
    }
}
