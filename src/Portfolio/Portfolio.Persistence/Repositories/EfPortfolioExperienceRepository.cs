using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Persistence.Repositories;

public class EfPortfolioExperienceRepository : BasePortfolioRepository<Experience, ExperienceEntity>, IPortfolioExperienceRepository
{
    public EfPortfolioExperienceRepository(PortfolioContext context)
        : base(context, ExperienceMapper.ToDomain) { }

    public async Task<List<Experience>> GetAllExperienceWithProjectsAsync(CancellationToken cancellationToken)
    {
        var experienceEntities = await _dbSet.AsNoTracking()
                                             .AsSplitQuery()
                                             .Include(e => e.Projects)
                                             .ThenInclude(e => e.ProjectTechStacks)
                                             .ThenInclude(e => e.TechStack)
                                             .ThenInclude(e => e.TechStackCategory)
                                             .ToListAsync(cancellationToken);

        return experienceEntities.Select(ExperienceMapper.ToDomain).ToList();
    }
}
