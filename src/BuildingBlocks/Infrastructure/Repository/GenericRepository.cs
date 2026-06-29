using System.Linq.Expressions;
using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.BuildingBlocks.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.BuildingBlocks.Infrastructure.Repository;

public class GenericRepository<T, TContext>(TContext context) : IRepository<T>
    where T : BaseEntity
    where TContext : DbContext
{
    protected readonly TContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _dbSet.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await _dbSet.ToListAsync(ct);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _dbSet.Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _dbSet.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate == null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _dbSet.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) =>
        await _dbSet.AddRangeAsync(entities, ct);

    public void Update(T entity) =>
        _dbSet.Update(entity);

    public void Remove(T entity) =>
        _dbSet.Remove(entity);

    public void SoftDelete(T entity)
    {
        if (entity is AuditableEntity auditable)
        {
            auditable.IsDeleted = true;
            auditable.DeletedAt = DateTime.UtcNow;
        }
        _dbSet.Update(entity);
    }

    public IQueryable<T> Query() => _dbSet.AsQueryable();
}
