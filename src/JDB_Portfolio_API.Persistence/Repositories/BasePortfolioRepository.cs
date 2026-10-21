using AutoMapper;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Persistence.Repositories;

public abstract class BasePortfolioRepository<TDomain, TEntity> : IRepository<TDomain>
    where TDomain : class
    where TEntity : class
{
    protected readonly PortfolioContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly IMapper _mapper;

    protected BasePortfolioRepository(PortfolioContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<List<TDomain>> GetAllAsync()
    {
        var entities = await _dbSet.AsNoTracking().ToListAsync();

        return _mapper.Map<List<TDomain>>(entities);
    }
}
