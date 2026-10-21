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

public class EfPortfolioProjectRepository: BasePortfolioRepository<Project, ProjectEntity>, IPortfolioProjectRepository
{
    public EfPortfolioProjectRepository(PortfolioContext context, IMapper mapper)
        : base(context, mapper) { }

    public async Task<List<Project>> GetAllProjectsWithTechStacksAsync()
    {
        var projectEntities = await _dbSet.AsNoTracking()
                                          .AsSplitQuery()
                                          .Include(p => p.ProjectTechStacks)
                                          .ThenInclude(pt => pt.TechStack)
                                          .ThenInclude(ts => ts.TechStackCategory)
                                          .ToListAsync();
        return _mapper.Map<List<Project>>(projectEntities);
    }
}
