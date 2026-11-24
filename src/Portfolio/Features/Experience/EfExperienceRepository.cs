using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// EF adapter for the Experience port. Reads (including the anonymous detail lookups
/// and pre-write checks) use the read-only context; admin writes use the DML credential.
/// The Projects navigation is a read-only projection — a write never touches it, because
/// a project owns its own experience_id (ADR-0003).
/// </summary>
public sealed class EfExperienceRepository
    : BasePortfolioRepository<DomainExperience, ExperienceEntity>, IExperienceRepository
{
    private readonly PortfolioWriteContext _writeContext;

    public EfExperienceRepository(
        PortfolioContext context,
        PortfolioWriteContext writeContext)
        : base(context, ExperienceMapper.ToDomain) =>
        _writeContext = writeContext;

    public async Task<List<DomainExperience>> GetAllWithProjectsAsync(CancellationToken cancellationToken)
    {
        var experienceEntities = await WithProjects().ToListAsync(cancellationToken);

        return experienceEntities.Select(ExperienceMapper.ToDomain).ToList();
    }

    public async Task<DomainExperience?> GetByIdAsync(
        int id, CancellationToken cancellationToken)
    {
        var entity = await WithProjects()
            .SingleOrDefaultAsync(experience => experience.Id == id, cancellationToken);

        return entity is null ? null : ExperienceMapper.ToDomain(entity);
    }

    public async Task<DomainExperience> AddAsync(
        DomainExperience experience, CancellationToken cancellationToken)
    {
        var entity = ExperienceMapper.ToEntity(experience);

        _writeContext.Experiences.Add(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return ExperienceMapper.ToDomain(entity);
    }

    public async Task<DomainExperience> UpdateAsync(
        DomainExperience experience, CancellationToken cancellationToken)
    {
        var entity = ExperienceMapper.ToEntity(experience);

        // Mark only the root row modified. Update() would also attach the empty
        // Projects collection, which risks rewriting the project FK this slice
        // does not own.
        _writeContext.Experiences.Attach(entity);
        _writeContext.Entry(entity).State = EntityState.Modified;
        await _writeContext.SaveChangesAsync(cancellationToken);

        return ExperienceMapper.ToDomain(entity);
    }

    public async Task DeleteAsync(
        DomainExperience experience, CancellationToken cancellationToken)
    {
        // project.experience_id is ON DELETE SET NULL, so the projects survive as
        // standalone rows (ADR-0003).
        _writeContext.Experiences.Remove(new ExperienceEntity { Id = experience.Id });
        await _writeContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ExperienceEntity> WithProjects() =>
        _dbSet.AsNoTracking()
            .AsSplitQuery()
            .Include(experience => experience.Projects)
            .ThenInclude(project => project.ProjectTechStacks)
            .ThenInclude(join => join.TechStack)
            .ThenInclude(stack => stack.TechStackCategory);
}
