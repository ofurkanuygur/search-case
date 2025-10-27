using SearchCase.Contracts.Models;

namespace SearchCase.Contracts.Mapping;

/// <summary>
/// Generic interface for mapping provider-specific DTOs to canonical content
/// </summary>
/// <typeparam name="TSource">The provider-specific DTO type</typeparam>
public interface IContentMapper<in TSource>
{
    /// <summary>
    /// Maps provider-specific content to canonical format
    /// </summary>
    /// <param name="source">The source DTO from the provider</param>
    /// <param name="providerId">The identifier of the source provider</param>
    /// <returns>Mapping result with canonical content or errors</returns>
    MappingResult<CanonicalContent> MapToCanonical(TSource source, string providerId);
}
