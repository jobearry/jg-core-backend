using Portfolio.Domain.Interfaces;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// The Experience slice's port. It lives with the feature so the slice owns both
/// sides of the boundary; the domain entity it returns is still the shared one.
/// </summary>
public interface IExperienceRepository : IRepository<DomainExperience>
{
    Task<List<DomainExperience>> GetAllWithProjectsAsync(CancellationToken cancellationToken);
}
