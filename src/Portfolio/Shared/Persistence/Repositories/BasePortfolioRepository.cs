using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Persistence.Repositories;

public abstract class BasePortfolioRepository<TDomain, TEntity> : IRepository<TDomain>
    where TDomain : class
    where TEntity : class
{
    protected readonly PortfolioContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    private readonly Func<TEntity, TDomain> _map;

    protected BasePortfolioRepository(PortfolioContext context, Func<TEntity, TDomain> map)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
        _map = map;
    }

    public virtual async Task<List<TDomain>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbSet.AsNoTracking().ToListAsync(cancellationToken);

        return entities.Select(_map).ToList();
    }
}
