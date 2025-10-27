using System.Xml.Serialization;
using XmlProviderMicroservice.Configuration;
using XmlProviderMicroservice.Models;
using Microsoft.Extensions.Options;

namespace XmlProviderMicroservice.HttpClients;

public sealed class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(HttpClient httpClient, IOptions<ExternalApiSettings> settings, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<FeedResponse> GetFeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_settings.EnableDetailedLogging) _logger.LogInformation("Fetching XML from {Url}", _settings.BaseUrl);
            
            var response = await _httpClient.GetAsync(_settings.BaseUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var serializer = new XmlSerializer(typeof(FeedResponse));
            using var reader = new StringReader(xml);
            var result = (FeedResponse?)serializer.Deserialize(reader) ?? new FeedResponse();
            
            _logger.LogInformation("Successfully fetched {Count} items from XML API", result.Items.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching XML from {Url}", _settings.BaseUrl);
            throw;
        }
    }
}
