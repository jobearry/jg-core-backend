using Microsoft.EntityFrameworkCore;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// EF adapter for the Project port. The tech-stack graph is loaded only when the
/// caller asks for tools; otherwise the base repository reads the plain rows.
/// </summary>
public sealed class EfProjectRepository
    : BasePortfolioRepository<DomainProject, ProjectEntity>, IProjectRepository
{
    public EfProjectRepository(PortfolioContext context)
        : base(context, ProjectMapper.ToDomain) { }

    public async Task<List<DomainProject>> GetAllWithTechStacksAsync(CancellationToken cancellationToken)
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
