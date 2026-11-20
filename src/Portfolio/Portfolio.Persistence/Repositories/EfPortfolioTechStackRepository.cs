using AutoMapper;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Portfolio.Persistence.Repositories;

public class EfPortfolioTechStackRepository: BasePortfolioRepository<TechStack,TechStackEntity>, IPortfolioTechStackRepository
{
    public EfPortfolioTechStackRepository(PortfolioContext context, IMapper mapper)
        : base(context, mapper) { }

    public async Task<List<TechStack>> GetAllTechStacksAsync(CancellationToken cancellationToken)
    {
        var techStackEntities = await _dbSet.AsNoTracking()
                                            .Include(ts => ts.TechStackCategory)
                                            .ToListAsync(cancellationToken);

        return _mapper.Map<List<TechStack>>(techStackEntities);
    }
}
