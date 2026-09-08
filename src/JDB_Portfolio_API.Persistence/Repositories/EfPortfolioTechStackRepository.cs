using AutoMapper;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Contexts;
using JDB_Portfolio_API.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Persistence.Repositories;

public class EfPortfolioTechStackRepository: BasePortfolioRepository<TechStack,TechStackEntity>, IPortfolioTechStackRepository
{
    public EfPortfolioTechStackRepository(PortfolioContext context, IMapper mapper)
        : base(context, mapper) { }

    public async Task<List<TechStack?>> GetAllTechStacksAsync()
    {
        var techStackEntities = await _dbSet.Include(ts => ts.TechStackCategory)
                                            .ToListAsync();

        return _mapper.Map<List<TechStack?>>(techStackEntities);
    }
}
