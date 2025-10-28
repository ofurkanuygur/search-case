using SearchCase.Contracts.Models;
using WriteService.Application.DTOs;

namespace WriteService.Application.Services;

/// <summary>
/// Service for detecting changes in content
/// Compares hashes to identify NEW, UPDATED, and UNCHANGED items
/// </summary>
public interface IChangeDetectionService
{
    /// <summary>
    /// Detects changes in a list of canonical content
    /// </summary>
    /// <param name="contents">Content items to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ChangeDetectionResult categorizing items</returns>
    Task<ChangeDetectionResult> DetectChangesAsync(
        List<CanonicalContent> contents,
        CancellationToken cancellationToken = default);
}
