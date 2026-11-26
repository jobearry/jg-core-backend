using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Features.Skill;

/// <summary>
/// The port for tech stack categories, which are shared reference data with their own
/// lifecycle (ADR-0003). A stack write resolves its category through this port.
/// </summary>
public interface ITechStackCategoryRepository : IRepository<TechStackCategory>
{
    Task<TechStackCategory?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<TechStackCategory?> GetByNameAsync(string category, CancellationToken cancellationToken);

    Task<TechStackCategory> AddAsync(TechStackCategory category, CancellationToken cancellationToken);

    Task<TechStackCategory> UpdateAsync(TechStackCategory category, CancellationToken cancellationToken);

    Task<bool> HasStacksAsync(int categoryId, CancellationToken cancellationToken);

    Task DeleteAsync(TechStackCategory category, CancellationToken cancellationToken);
}
