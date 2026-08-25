using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for reading savings certificate data from the database.
/// </summary>
/// <param name="dbContext">The database context used for data access.</param>
public sealed class SavingsCertificateReadRepository(DenariusDbContext dbContext) : ISavingsCertificateReadRepository
{
    /// <summary>
    /// Retrieves a read-only list of all savings certificates ordered by investment date.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of savings certificate summary DTOs.</returns>
    public async Task<IReadOnlyList<SavingsCertificateSummaryDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SavingsCertificates.AsNoTracking().OrderBy(item => item.InvestmentDate)
            .Select(item => new SavingsCertificateSummaryDto(item.Id, item.InvestmentDate, item.SeriesNumber,
                item.Description, item.InvestmentValue, item.Rate, item.CurrentValue, item.NextCapitalization))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Calculates the total current value of all savings certificates, optionally filtered by a specific date.
    /// </summary>
    /// <param name="at">Optional date to filter certificates by investment date. If null, includes all certificates.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The sum of current values of all matching savings certificates.</returns>
    public Task<decimal> GetCurrentValueAsync(DateOnly? at = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SavingsCertificates.AsNoTracking();
        if (at.HasValue) query = query.Where(item => item.InvestmentDate <= at.Value);
        return query.SumAsync(item => item.CurrentValue, cancellationToken);
    }
}
