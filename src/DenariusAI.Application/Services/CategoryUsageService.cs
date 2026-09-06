using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;

namespace DenariusAI.Application.Services;

/// <summary>
/// Provides read-only category usage information derived from journal entry lines.
/// </summary>
/// <param name="unitOfWork">The unit of work used to query persisted journal entry lines.</param>
public sealed class CategoryUsageService(IUnitOfWork unitOfWork) : ICategoryUsageService
{
    /// <summary>
    /// Gets the requested category identifiers that have been referenced by at least one journal entry line.
    /// Historical lines are intentionally included because the result represents whether a category has ever been used.
    /// </summary>
    /// <param name="categoryIds">Category identifiers to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subset of category identifiers that have movement usage.</returns>
    public async Task<IReadOnlySet<Guid>> GetUsedInJournalMovementsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);

        var ids = categoryIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0) return new HashSet<Guid>();

        var lines = await unitOfWork.Repository<JournalEntryLine>().FindAsync(
            line => line.CategoryId.HasValue && ids.Contains(line.CategoryId.Value), cancellationToken);

        return lines.Where(line => line.CategoryId.HasValue)
            .Select(line => line.CategoryId!.Value)
            .ToHashSet();
    }
}
