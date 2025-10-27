using XmlProviderMicroservice.Models;

namespace XmlProviderMicroservice.HttpClients;

public interface IExternalApiClient
{
    Task<FeedResponse> GetFeedAsync(CancellationToken cancellationToken = default);
}
