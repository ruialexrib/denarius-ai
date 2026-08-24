using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Domain.Common;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

public sealed class UnitOfWork(
    DenariusDbContext dbContext,
    IAccountRepository accounts,
    IJournalEntryRepository journalEntries,
    IBudgetRepository budgets) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];
    public IAccountRepository Accounts { get; } = accounts;
    public IJournalEntryRepository JournalEntries { get; } = journalEntries;
    public IBudgetRepository Budgets { get; } = budgets;

    public IRepository<T> Repository<T>() where T : AuditableEntity
    {
        if (_repositories.TryGetValue(typeof(T), out var repository)) return (IRepository<T>)repository;
        var created = new Repository<T>(dbContext); _repositories[typeof(T)] = created; return created;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

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
