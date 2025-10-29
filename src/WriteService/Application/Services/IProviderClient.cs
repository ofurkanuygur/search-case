using SearchCase.Contracts.Responses;

namespace WriteService.Application.Services;

/// <summary>
/// Interface for calling provider microservices
/// Abstracts HTTP communication with JSON and XML providers
/// </summary>
public interface IProviderClient
{
    /// <summary>
    /// Fetches data from JSON Provider microservice
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProviderResponse with canonical content</returns>
    Task<ProviderResponse?> FetchFromJsonProviderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches data from XML Provider microservice
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProviderResponse with canonical content</returns>
    Task<ProviderResponse?> FetchFromXmlProviderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches data from both providers and merges results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged ProviderResponse from both providers</returns>
    Task<ProviderResponse> FetchFromAllProvidersAsync(CancellationToken cancellationToken = default);
}
