using Portfolio.Domain.Interfaces;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// The Experience slice's port and the aggregate's only write entry point. It lives
/// with the feature so the slice owns both sides of the boundary; the domain entity
/// it returns is still the shared one. Reads use the read-only credential, writes
/// use the writer, and the Projects projection is never written here (ADR-0003).
/// </summary>
public interface IExperienceRepository : IRepository<DomainExperience>
{
    Task<List<DomainExperience>> GetAllWithProjectsAsync(CancellationToken cancellationToken);

    Task<DomainExperience?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<DomainExperience> AddAsync(DomainExperience experience, CancellationToken cancellationToken);

    Task<DomainExperience> UpdateAsync(DomainExperience experience, CancellationToken cancellationToken);

    Task DeleteAsync(DomainExperience experience, CancellationToken cancellationToken);
}
