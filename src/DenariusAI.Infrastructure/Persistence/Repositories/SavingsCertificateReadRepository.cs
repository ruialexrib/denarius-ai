using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

public sealed class SavingsCertificateReadRepository(DenariusDbContext dbContext) : ISavingsCertificateReadRepository
{
    public async Task<IReadOnlyList<SavingsCertificateSummaryDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SavingsCertificates.AsNoTracking().OrderBy(item => item.InvestmentDate)
            .Select(item => new SavingsCertificateSummaryDto(item.Id, item.InvestmentDate, item.SeriesNumber,
                item.Description, item.InvestmentValue, item.Rate, item.CurrentValue, item.NextCapitalization))
            .ToListAsync(cancellationToken);

    public Task<decimal> GetCurrentValueAsync(DateOnly? at = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SavingsCertificates.AsNoTracking();
        if (at.HasValue) query = query.Where(item => item.InvestmentDate <= at.Value);
        return query.SumAsync(item => item.CurrentValue, cancellationToken);
    }
}
