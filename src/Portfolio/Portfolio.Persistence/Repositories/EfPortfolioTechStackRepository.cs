using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Persistence.Repositories;

public class EfPortfolioTechStackRepository : BasePortfolioRepository<TechStack, TechStackEntity>, IPortfolioTechStackRepository
{
    public EfPortfolioTechStackRepository(PortfolioContext context)
        : base(context, TechStackMapper.ToDomain) { }

    public async Task<List<TechStack>> GetAllTechStacksAsync(CancellationToken cancellationToken)
    {
        var techStackEntities = await _dbSet.AsNoTracking()
                                            .Include(ts => ts.TechStackCategory)
                                            .ToListAsync(cancellationToken);

        return techStackEntities.Select(TechStackMapper.ToDomain).ToList();
    }
}
