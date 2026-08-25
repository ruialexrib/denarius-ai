using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Domain.Common;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Implements the Unit of Work pattern to manage database transactions and coordinate repository operations.
/// </summary>
/// <param name="dbContext">The database context instance.</param>
/// <param name="accounts">The account repository instance.</param>
/// <param name="journalEntries">The journal entry repository instance.</param>
/// <param name="budgets">The budget repository instance.</param>
public sealed class UnitOfWork(
    DenariusDbContext dbContext,
    IAccountRepository accounts,
    IJournalEntryRepository journalEntries,
    IBudgetRepository budgets) : IUnitOfWork
{
    /// <summary>
    /// Cache of dynamically created repositories.
    /// </summary>
    private readonly Dictionary<Type, object> _repositories = [];

    /// <summary>
    /// Gets the account repository.
    /// </summary>
    public IAccountRepository Accounts { get; } = accounts;

    /// <summary>
    /// Gets the journal entry repository.
    /// </summary>
    public IJournalEntryRepository JournalEntries { get; } = journalEntries;

    /// <summary>
    /// Gets the budget repository.
    /// </summary>
    public IBudgetRepository Budgets { get; } = budgets;

    /// <summary>
    /// Gets or creates a generic repository for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from AuditableEntity.</typeparam>
    /// <returns>A repository instance for the specified entity type.</returns>
    public IRepository<T> Repository<T>() where T : AuditableEntity
    {
        if (_repositories.TryGetValue(typeof(T), out var repository)) return (IRepository<T>)repository;
        var created = new Repository<T>(dbContext); _repositories[typeof(T)] = created; return created;
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Executes an operation within a database transaction, with automatic rollback on failure.
    /// </summary>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the operation is null.</exception>
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (!dbContext.Database.IsRelational()) { await operation(cancellationToken); return; }
        if (dbContext.Database.CurrentTransaction is not null) { await operation(cancellationToken); return; }
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try { await operation(cancellationToken); await transaction.CommitAsync(cancellationToken); }
            catch { await transaction.RollbackAsync(cancellationToken); throw; }
        });
    }
}
