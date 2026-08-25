using DenariusAI.Domain.Common;

namespace DenariusAI.Application.Abstractions.Persistence;

/// <summary>
/// Defines the Unit of Work pattern for managing database transactions and repository access.
/// Coordinates the work of multiple repositories by creating a single database context shared by all.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets the repository for managing Account entities.
    /// </summary>
    IAccountRepository Accounts { get; }
    
    /// <summary>
    /// Gets the repository for managing JournalEntry entities.
    /// </summary>
    IJournalEntryRepository JournalEntries { get; }
    
    /// <summary>
    /// Gets the repository for managing Budget entities.
    /// </summary>
    IBudgetRepository Budgets { get; }
    
    /// <summary>
    /// Gets a generic repository for any entity type that inherits from AuditableEntity.
    /// </summary>
    /// <typeparam name="T">The entity type that must inherit from AuditableEntity.</typeparam>
    /// <returns>A repository instance for the specified entity type.</returns>
    IRepository<T> Repository<T>() where T : AuditableEntity;
    
    /// <summary>
    /// Saves all pending changes to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an operation within a database transaction.
    /// If the operation succeeds, the transaction is committed; otherwise, it is rolled back.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute within the transaction.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
