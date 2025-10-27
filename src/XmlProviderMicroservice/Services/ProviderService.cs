using System.Diagnostics;
using SearchCase.Contracts.Mapping;
using SearchCase.Contracts.Responses;
using XmlProviderMicroservice.HttpClients;
using XmlProviderMicroservice.Models;

namespace XmlProviderMicroservice.Services;

/// <summary>
/// Service for managing provider data operations
/// </summary>
public sealed class ProviderService : IProviderService
{
    private readonly IExternalApiClient _apiClient;
    private readonly IContentMapper<Item> _mapper;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(
        IExternalApiClient apiClient,
        IContentMapper<Item> mapper,
        ILogger<ProviderService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves content data from the external provider and transforms to canonical format
    /// </summary>
    public async Task<ProviderResponse> GetDataAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Fetching data from XML provider");

        try
        {
            // Fetch from external API
            var externalData = await _apiClient.GetFeedAsync(cancellationToken);

            // Transform to canonical format
            var canonicalItems = new List<SearchCase.Contracts.Models.CanonicalContent>();
            var errors = new List<string>();

            foreach (var item in externalData.Items)
            {
                var mappingResult = _mapper.MapToCanonical(item, "provider2");

                if (mappingResult.Success && mappingResult.Data != null)
                {
                    canonicalItems.Add(mappingResult.Data);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to map content {ContentId}: {Errors}",
                        item.Id,
                        string.Join(", ", mappingResult.Errors));

                    errors.AddRange(mappingResult.Errors);
                }
            }

            sw.Stop();

            _logger.LogInformation(
                "Successfully transformed {SuccessCount}/{TotalCount} items in {Duration}ms",
                canonicalItems.Count,
                externalData.Items.Count,
                sw.ElapsedMilliseconds);

            return new ProviderResponse
            {
                Items = canonicalItems,
                Pagination = new PaginationMetadata
                {
                    TotalItems = externalData.Meta.TotalCount,
                    CurrentPage = externalData.Meta.CurrentPage,
                    PageSize = externalData.Meta.ItemsPerPage
                },
                Provider = new ProviderMetadata
                {
                    ProviderId = "provider2",
                    ProviderName = "XML Provider",
                    FetchedAt = DateTimeOffset.UtcNow,
                    FetchDuration = sw.Elapsed
                },
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from XML provider");
            throw;
        }
    }
}
