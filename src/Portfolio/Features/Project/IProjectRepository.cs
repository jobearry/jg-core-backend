using Portfolio.Domain.Interfaces;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// The Project slice's port. The overview handler also reads through it, but the
/// contract stays here with the feature it belongs to.
/// </summary>
public interface IProjectRepository : IRepository<DomainProject>
{
    Task<List<DomainProject>> GetAllWithTechStacksAsync(CancellationToken cancellationToken);
}
