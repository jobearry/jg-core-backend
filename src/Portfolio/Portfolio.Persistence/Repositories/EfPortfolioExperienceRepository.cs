using AutoMapper;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Portfolio.Persistence.Repositories;

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
