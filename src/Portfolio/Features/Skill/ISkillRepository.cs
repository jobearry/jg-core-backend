using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Features.Skill;

/// <summary>
/// The Skill slice's port. Reads cover the whole collection or one row; the write
/// methods take the category id separately because the domain holds only its name.
/// </summary>
public interface ISkillRepository : IRepository<TechStack>
{
    Task<TechStack?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<TechStack> AddAsync(TechStack stack, int categoryId, CancellationToken cancellationToken);

    Task<TechStack> UpdateAsync(TechStack stack, int categoryId, CancellationToken cancellationToken);

    Task<bool> IsUsedByProjectsAsync(int stackId, CancellationToken cancellationToken);

    Task DeleteAsync(TechStack stack, CancellationToken cancellationToken);
}
