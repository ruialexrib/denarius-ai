using DenariusAI.Domain.Common;

namespace DenariusAI.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    IAccountRepository Accounts { get; }
    IJournalEntryRepository JournalEntries { get; }
    IBudgetRepository Budgets { get; }
    IRepository<T> Repository<T>() where T : AuditableEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
