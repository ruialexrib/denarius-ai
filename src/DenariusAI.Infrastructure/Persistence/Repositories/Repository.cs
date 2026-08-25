using System.Linq.Expressions;
using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation for managing entities that inherit from <see cref="AuditableEntity"/>.
/// Provides basic CRUD operations and query capabilities.
/// </summary>
/// <typeparam name="T">The entity type that inherits from <see cref="AuditableEntity"/>.</typeparam>
/// <param name="dbContext">The database context instance.</param>
public class Repository<T>(DenariusDbContext dbContext) : IRepository<T> where T : AuditableEntity
{
    /// <summary>
    /// Gets the database context instance.
    /// </summary>
    protected DenariusDbContext DbContext { get; } = dbContext;

    /// <summary>
    /// Gets the DbSet for the entity type.
    /// </summary>
    protected DbSet<T> Set { get; } = dbContext.Set<T>();

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    /// <summary>
    /// Retrieves all entities as a read-only list.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of all entities.</returns>
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);

    /// <summary>
    /// Finds entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of entities that match the predicate.</returns>
    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    /// <summary>
    /// Gets a queryable collection of entities.
    /// </summary>
    /// <param name="asNoTracking">If true, returns an untracked queryable; otherwise, a tracked queryable.</param>
    /// <returns>An <see cref="IQueryable{T}"/> for the entity type.</returns>
    public IQueryable<T> Query(bool asNoTracking = true) => asNoTracking ? Set.AsNoTracking() : Set;

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddAsync(T entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public void Update(T entity) => Set.Update(entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(T entity) => Set.Remove(entity);

    /// <summary>
    /// Checks if any entity exists that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any entity matches the predicate; otherwise, false.</returns>
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Set.AnyAsync(predicate, cancellationToken);
}
