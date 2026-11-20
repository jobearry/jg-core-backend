using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Persistence.Repositories;

public class EfPortfolioProjectRepository : BasePortfolioRepository<Project, ProjectEntity>, IPortfolioProjectRepository
{
    public EfPortfolioProjectRepository(PortfolioContext context)
        : base(context, ProjectMapper.ToDomain) { }

    public async Task<List<Project>> GetAllProjectsWithTechStacksAsync(CancellationToken cancellationToken)
    {
        var projectEntities = await _dbSet.AsNoTracking()
                                          .AsSplitQuery()
                                          .Include(p => p.ProjectTechStacks)
                                          .ThenInclude(pt => pt.TechStack)
                                          .ThenInclude(ts => ts.TechStackCategory)
                                          .ToListAsync(cancellationToken);
        return projectEntities.Select(ProjectMapper.ToDomain).ToList();
    }
}
