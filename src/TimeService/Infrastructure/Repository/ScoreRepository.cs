using Microsoft.EntityFrameworkCore;
using Npgsql;
using TimeService.Data;
using TimeService.Domain.Entities;

namespace TimeService.Infrastructure.Repository;

/// <summary>
/// EF Core + Raw SQL repository for score update operations
/// Implements optimized bulk updates for performance
/// SOLID: Single Responsibility - Only handles score database operations
/// </summary>
public sealed class ScoreRepository : IScoreRepository
{
    private readonly TimeServiceDbContext _context;
    private readonly ILogger<ScoreRepository> _logger;

    public ScoreRepository(
        TimeServiceDbContext context,
        ILogger<ScoreRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ContentEntity>> GetAllContentAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching all content from database");

        var content = await _context.Contents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Fetched {Count} content items", content.Count);

        return content;
    }

    public async Task<List<ContentEntity>> GetContentByPublishDatesAsync(
        List<DateTimeOffset> publishDates,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching content published on {Count} specific dates",
            publishDates.Count);

        // Convert to date-only for comparison
        var dates = publishDates.Select(d => d.Date).ToList();

        var content = await _context.Contents
            .AsNoTracking()
            .Where(c => dates.Contains(c.PublishedAt.Date))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {Count} content items crossing freshness thresholds",
            content.Count);

        return content;
    }

    public async Task<int> BulkUpdateScoresAsync(
        List<ContentEntity> contentUpdates,
        CancellationToken cancellationToken = default)
    {
        if (contentUpdates == null || contentUpdates.Count == 0)
        {
            _logger.LogWarning("BulkUpdateScoresAsync called with empty list");
            return 0;
        }

        _logger.LogInformation(
            "Starting bulk score update for {Count} content items",
            contentUpdates.Count);

        try
        {
            // Use raw SQL for optimal performance with large updates
            // UPDATE contents SET
            //   final_score = @newScore,
            //   fresh_score = @freshScore,
            //   updated_at = NOW()
            // WHERE id = @id

            var affectedRows = 0;

            // Batch updates in transaction for consistency
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var content in contentUpdates)
                {
                    var result = await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE contents
                          SET score = @p0,
                              updated_at = @p1
                          WHERE id = @p2",
                        [
                            content.Score.Value,
                            DateTimeOffset.UtcNow,
                            content.Id
                        ],
                        cancellationToken);

                    affectedRows += result;
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Bulk score update completed successfully: {Affected} rows affected",
                    affectedRows);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return affectedRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk score update");
            throw;
        }
    }

    public async Task<bool> UpdateScoreAsync(
        ContentEntity content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE contents
                  SET score = @p0,
                      updated_at = @p1
                  WHERE id = @p2",
                [
                    content.Score.Value,
                    DateTimeOffset.UtcNow,
                    content.Id
                ],
                cancellationToken);

            var success = affectedRows > 0;

            if (success)
            {
                _logger.LogDebug(
                    "Updated score for {ContentId}: Score={Score}",
                    content.Id,
                    content.Score.Value);
            }
            else
            {
                _logger.LogWarning("No rows affected when updating score for {ContentId}", content.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating score for {ContentId}", content.Id);
            return false;
        }
    }
}
