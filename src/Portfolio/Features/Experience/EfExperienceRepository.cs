using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// EF adapter for the Experience port. The query includes the project graph only
/// when the caller asks for it; the plain read stays on the base repository.
/// </summary>
public sealed class EfExperienceRepository
    : BasePortfolioRepository<DomainExperience, ExperienceEntity>, IExperienceRepository
{
    public EfExperienceRepository(PortfolioContext context)
        : base(context, ExperienceMapper.ToDomain) { }

    public async Task<List<DomainExperience>> GetAllWithProjectsAsync(CancellationToken cancellationToken)
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
