using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// The Project slice's port and the aggregate's only write entry point (ADR-0003).
/// The overview handler also reads through it, but the contract stays here with the
/// feature it belongs to. Reads use the read-only credential; writes use the writer.
/// </summary>
public interface IProjectRepository : IRepository<DomainProject>
{
    Task<List<DomainProject>> GetAllWithTechStacksAsync(CancellationToken cancellationToken);

    Task<DomainProject?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<DomainProject> AddAsync(DomainProject project, CancellationToken cancellationToken);

    Task<DomainProject> UpdateAsync(DomainProject project, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the project_tech_stack rows for this project. The refs must already
    /// have been resolved from the shared stack rows, so no name or category is written.
    /// </summary>
    Task<DomainProject> ReplaceTechStacksAsync(
        DomainProject project, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a single tech stack link. The ref must already be resolved.
    /// </summary>
    Task<DomainProject> AddTechStackAsync(
        DomainProject project, TechStackRef techStackRef, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a single tech stack link.
    /// </summary>
    Task<DomainProject> RemoveTechStackAsync(
        DomainProject project, int techStackId, CancellationToken cancellationToken);

    Task DeleteAsync(DomainProject project, CancellationToken cancellationToken);
}
