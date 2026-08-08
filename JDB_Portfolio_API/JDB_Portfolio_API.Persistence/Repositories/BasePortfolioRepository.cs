using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace JDB_Portfolio_API.Persistence.Repositories;

public class BasePortfolioRepository<TDomain, TEntity> : IRepository<TDomain> 
    where TDomain: class
    where TEntity: class
{
    public readonly PortfolioContext _context;
    public readonly DbSet<TEntity> _dbSet;
    public readonly IMapper _mapper;
    public BasePortfolioRepository(PortfolioContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<int> GetCountAsync() => await _dbSet.CountAsync();
    public async Task<List<TDomain>> GetAllAsync()
    {
        var entities = await _dbSet.ToListAsync();
        return _mapper.Map<List<TDomain>>(entities);
    }
    public async Task<TDomain?> GetByIdAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        return _mapper.Map<TDomain?>(entity);
    }
    //public async Task AddNewAsync(TDomain domain)
    //{
    //    var entity = _mapper.Map<TEntity>(domain);
    //    await _dbSet.AddAsync(entity);
    //    await _context.SaveChangesAsync();
    //}
}
