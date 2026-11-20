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

public class EfPortfolioProjectRepository: BasePortfolioRepository<Project, ProjectEntity>, IPortfolioProjectRepository
{
    public EfPortfolioProjectRepository(PortfolioContext context, IMapper mapper)
        : base(context, mapper) { }

    public async Task<List<Project>> GetAllProjectsWithTechStacksAsync(CancellationToken cancellationToken)
    {
        var projectEntities = await _dbSet.AsNoTracking()
                                          .AsSplitQuery()
                                          .Include(p => p.ProjectTechStacks)
                                          .ThenInclude(pt => pt.TechStack)
                                          .ThenInclude(ts => ts.TechStackCategory)
                                          .ToListAsync(cancellationToken);
        return _mapper.Map<List<Project>>(projectEntities);
    }
}
