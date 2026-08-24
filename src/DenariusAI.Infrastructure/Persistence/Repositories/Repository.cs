using System.Linq.Expressions;
using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

public class Repository<T>(DenariusDbContext dbContext) : IRepository<T> where T : AuditableEntity
{
    protected DenariusDbContext DbContext { get; } = dbContext;
    protected DbSet<T> Set { get; } = dbContext.Set<T>();

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public IQueryable<T> Query(bool asNoTracking = true) => asNoTracking ? Set.AsNoTracking() : Set;
    public Task AddAsync(T entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Set.AnyAsync(predicate, cancellationToken);
}
