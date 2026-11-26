using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// EF adapter for the Project port. Reads (including the anonymous detail lookups and
/// pre-write checks) use the read-only context; admin writes use the DML credential.
/// The many-to-many rows have one explicit method; the scalar update leaves them alone.
/// </summary>
public sealed class EfProjectRepository
    : BasePortfolioRepository<DomainProject, ProjectEntity>, IProjectRepository
{
    private readonly PortfolioWriteContext _writeContext;
    private readonly TimeProvider _timeProvider;

    public EfProjectRepository(
        PortfolioContext context,
        PortfolioWriteContext writeContext,
        TimeProvider timeProvider)
        : base(context, ProjectMapper.ToDomain)
    {
        _writeContext = writeContext;
        _timeProvider = timeProvider;
    }

    public async Task<List<DomainProject>> GetAllWithTechStacksAsync(CancellationToken cancellationToken)
    {
        var projectEntities = await WithTechStacks().ToListAsync(cancellationToken);

        return projectEntities.Select(ProjectMapper.ToDomain).ToList();
    }

    public async Task<DomainProject?> GetByIdAsync(
        int id, CancellationToken cancellationToken)
    {
        // Includes the stack refs so the anonymous detail GET publishes the same
        // shape as the collection with includeTools=true.
        var entity = await WithTechStacks()
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);

        return entity is null ? null : ProjectMapper.ToDomain(entity);
    }

    private IQueryable<ProjectEntity> WithTechStacks() =>
        _dbSet.AsNoTracking()
            .AsSplitQuery()
            .Include(project => project.ProjectTechStacks)
            .ThenInclude(join => join.TechStack)
            .ThenInclude(stack => stack.TechStackCategory);

    public async Task<DomainProject> AddAsync(
        DomainProject project, CancellationToken cancellationToken)
    {
        var entity = ProjectMapper.ToEntity(project);

        _writeContext.Projects.Add(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return ProjectMapper.ToDomain(entity);
    }

    public async Task<DomainProject> UpdateAsync(
        DomainProject project, CancellationToken cancellationToken)
    {
        var entity = ProjectMapper.ToEntity(project);
        StampUpdatedAt(entity);

        // Update() would also attach the empty ProjectTechStacks collection, which
        // risks rewriting join rows this slice does not own. Mark only the columns
        // this command owns; CreatedAt belongs to the database default.
        _writeContext.Projects.Attach(entity);
        var entry = _writeContext.Entry(entity);
        entry.Property(e => e.ExperienceId).IsModified = true;
        entry.Property(e => e.ProjectName).IsModified = true;
        entry.Property(e => e.Description).IsModified = true;
        entry.Property(e => e.Contribution).IsModified = true;
        entry.Property(e => e.UpdatedAt).IsModified = true;
        await _writeContext.SaveChangesAsync(cancellationToken);

        return ProjectMapper.ToDomain(entity);
    }

    public async Task<DomainProject> ReplaceTechStacksAsync(
        DomainProject project, CancellationToken cancellationToken)
    {
        var entity = await _writeContext.Projects
            .Include(item => item.ProjectTechStacks)
            .SingleAsync(item => item.Id == project.Id, cancellationToken);

        entity.ProjectTechStacks.Clear();
        foreach (var join in ProjectMapper.ToJoinEntities(project))
        {
            entity.ProjectTechStacks.Add(join);
        }

        StampUpdatedAt(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        // The tracked joins have no TechStack navigation, so the returned DTO is
        // rebuilt through the read context where the category is loaded.
        return await GetByIdAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Project {project.Id} disappeared after its tech stacks were saved.");
    }

    public async Task<DomainProject> AddTechStackAsync(
        DomainProject project, TechStackRef techStackRef, CancellationToken cancellationToken)
    {
        var entity = await _writeContext.Projects
            .Include(item => item.ProjectTechStacks)
            .SingleAsync(item => item.Id == project.Id, cancellationToken);

        if (entity.ProjectTechStacks.Any(j => j.TechStackId == techStackRef.Id))
        {
            throw new InvalidOperationException($"Tech stack {techStackRef.Id} is already linked to project {project.Id}.");
        }

        entity.ProjectTechStacks.Add(ProjectMapper.ToJoinEntity(project.Id, techStackRef));
        StampUpdatedAt(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Project {project.Id} disappeared after tech stack was added.");
    }

    public async Task<DomainProject> RemoveTechStackAsync(
        DomainProject project, int techStackId, CancellationToken cancellationToken)
    {
        var entity = await _writeContext.Projects
            .Include(item => item.ProjectTechStacks)
            .SingleAsync(item => item.Id == project.Id, cancellationToken);

        var join = entity.ProjectTechStacks.FirstOrDefault(j => j.TechStackId == techStackId);
        if (join is null)
        {
            throw new InvalidOperationException($"Tech stack {techStackId} is not linked to project {project.Id}.");
        }

        entity.ProjectTechStacks.Remove(join);
        StampUpdatedAt(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Project {project.Id} disappeared after tech stack was removed.");
    }

    private void StampUpdatedAt(ProjectEntity entity) =>
        // updated_at is timestamp without time zone; Npgsql rejects Kind=Utc.
        entity.UpdatedAt = DateTime.SpecifyKind(
            _timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified);

    public async Task DeleteAsync(
        DomainProject project, CancellationToken cancellationToken)
    {
        _writeContext.Projects.Remove(new ProjectEntity { Id = project.Id });
        await _writeContext.SaveChangesAsync(cancellationToken);
    }
}
