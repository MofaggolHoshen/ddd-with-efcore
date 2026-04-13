using Microsoft.EntityFrameworkCore;
using OrderContext.Domain.Repositories;

namespace OrderContext.Infratructure.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// Provides base CRUD operations for all aggregate roots.
/// </summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class Repository<T, TId> : IRepository<T, TId> where T : class
{
    protected readonly OrderDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected Repository(OrderDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
