using System.Linq.Expressions;
using DenariusAI.Domain.Common;

namespace DenariusAI.Application.Abstractions.Persistence;

/// <summary>
/// Defines a generic repository interface for performing CRUD operations on entities that inherit from <see cref="AuditableEntity"/>.
/// Provides abstraction over data access operations with support for querying, filtering, and existence checks.
/// </summary>
/// <typeparam name="T">The entity type that inherits from <see cref="AuditableEntity"/>.</typeparam>
public interface IRepository<T> where T : AuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> Query(bool asNoTracking = true);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
