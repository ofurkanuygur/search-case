using SearchCase.Contracts.Responses;

namespace JsonProviderMicroservice.Services;

/// <summary>
/// Interface for provider service
/// </summary>
public interface IProviderService
{
    /// <summary>
    /// Retrieves content data from the external provider in canonical format
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider response with canonical content</returns>
    Task<ProviderResponse> GetDataAsync(CancellationToken cancellationToken = default);
}
