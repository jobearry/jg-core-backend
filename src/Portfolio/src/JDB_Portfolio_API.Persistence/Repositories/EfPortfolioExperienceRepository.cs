using AutoMapper;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using JDB_Portfolio_API.Persistence.Contexts;
using JDB_Portfolio_API.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Persistence.Repositories;

public class EfPortfolioExperienceRepository: BasePortfolioRepository<Experience, ExperienceEntity>, IPortfolioExperienceRepository
{
    public EfPortfolioExperienceRepository(PortfolioContext context, IMapper mapper)
        : base(context, mapper) { }

    public async Task<List<Experience>> GetAllExperienceWithProjectsAsync(CancellationToken cancellationToken)
    {
        var experienceEntities = await _dbSet.AsNoTracking()
                                             .AsSplitQuery()
                                             .Include(e => e.Projects)
                                             .ThenInclude(e => e.ProjectTechStacks)
                                             .ThenInclude(e => e.TechStack)
                                             .ThenInclude(e => e.TechStackCategory)
                                             .ToListAsync(cancellationToken);

        return _mapper.Map<List<Experience>>(experienceEntities);
    }
}
