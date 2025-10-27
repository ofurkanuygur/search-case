using JsonProviderMicroservice.Models;

namespace JsonProviderMicroservice.HttpClients;

/// <summary>
/// Interface for external API client
/// </summary>
public interface IExternalApiClient
{
    /// <summary>
    /// Fetches content data from the external provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content response from the provider</returns>
    Task<ContentResponse> GetContentAsync(CancellationToken cancellationToken = default);
}
