using System.Linq.Expressions;
using DenariusAI.Domain.Common;

namespace DenariusAI.Application.Abstractions.Persistence;

/// <summary>
/// Generic repository interface for data access operations on auditable entities.
/// Provides common CRUD operations and querying capabilities.
/// </summary>
/// <typeparam name="T">The entity type that inherits from AuditableEntity</typeparam>
public interface IRepository<T> where T : AuditableEntity
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The entity if found, otherwise null</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities of type T from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A read-only list of all entities</returns>
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A read-only list of entities matching the predicate</returns>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a queryable collection for advanced querying scenarios.
    /// </summary>
    /// <param name="asNoTracking">If true, entities are not tracked by the context</param>
    /// <returns>An IQueryable for composing queries</returns>
    IQueryable<T> Query(bool asNoTracking = true);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    void Update(T entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove</param>
    void Remove(T entity);

    /// <summary>
    /// Checks if any entity exists that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if at least one entity matches the predicate, otherwise false</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
