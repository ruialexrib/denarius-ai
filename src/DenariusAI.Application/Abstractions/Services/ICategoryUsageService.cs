namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Provides read-only category usage information for financial movements.
/// </summary>
public interface ICategoryUsageService
{
    /// <summary>
    /// Gets the category identifiers that have been referenced by at least one journal entry line.
    /// </summary>
    /// <param name="categoryIds">Category identifiers to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subset of category identifiers that have movement usage.</returns>
    Task<IReadOnlySet<Guid>> GetUsedInJournalMovementsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default);
}
