using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;

namespace Portfolio.Features.Skill;

/// <summary>
/// EF adapter for the Skill port. The category navigation is included because the
/// published shape flattens the category name onto each stack.
/// </summary>
public sealed class EfSkillRepository
    : BasePortfolioRepository<TechStack, TechStackEntity>, ISkillRepository
{
    public EfSkillRepository(PortfolioContext context)
        : base(context, TechStackMapper.ToDomain) { }

    public override async Task<List<TechStack>> GetAllAsync(CancellationToken cancellationToken)
    {
        var techStackEntities = await _dbSet.AsNoTracking()
                                            .Include(ts => ts.TechStackCategory)
                                            .ToListAsync(cancellationToken);

        return techStackEntities.Select(TechStackMapper.ToDomain).ToList();
    }
}
