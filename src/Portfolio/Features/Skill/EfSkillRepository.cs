using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;

namespace Portfolio.Features.Skill;

/// <summary>
/// EF adapter for the Skill port. Public reads use PortfolioContext with the category
/// navigation; admin writes use PortfolioWriteContext, so the read path keeps SELECT only.
/// </summary>
public sealed class EfSkillRepository
    : BasePortfolioRepository<TechStack, TechStackEntity>, ISkillRepository
{
    private readonly PortfolioWriteContext _writeContext;

    public EfSkillRepository(PortfolioContext context, PortfolioWriteContext writeContext)
        : base(context, TechStackMapper.ToDomain) =>
        _writeContext = writeContext;

    public override async Task<List<TechStack>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbSet.AsNoTracking()
            .Include(stack => stack.TechStackCategory)
            .ToListAsync(cancellationToken);

        return entities.Select(TechStackMapper.ToDomain).ToList();
    }

    public async Task<TechStack?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.AsNoTracking()
            .Include(stack => stack.TechStackCategory)
            .SingleOrDefaultAsync(stack => stack.Id == id, cancellationToken);

        return entity is null ? null : TechStackMapper.ToDomain(entity);
    }

    public async Task<TechStack> AddAsync(
        TechStack stack, int categoryId, CancellationToken cancellationToken)
    {
        var entity = TechStackMapper.ToEntity(stack, categoryId);

        _writeContext.TechStacks.Add(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return await MapWithCategoryAsync(entity, cancellationToken);
    }

    public async Task<TechStack> UpdateAsync(
        TechStack stack, int categoryId, CancellationToken cancellationToken)
    {
        var entity = TechStackMapper.ToEntity(stack, categoryId);

        // Update() would mark created_at modified and clear the database default.
        _writeContext.TechStacks.Attach(entity);
        var entry = _writeContext.Entry(entity);
        entry.Property(e => e.StackName).IsModified = true;
        entry.Property(e => e.ImgSrc).IsModified = true;
        entry.Property(e => e.TechStackCategoryId).IsModified = true;
        await _writeContext.SaveChangesAsync(cancellationToken);

        return await MapWithCategoryAsync(entity, cancellationToken);
    }

    public Task<bool> IsUsedByProjectsAsync(int stackId, CancellationToken cancellationToken) =>
        _context.Set<ProjectTechStack>()
            .AnyAsync(link => link.TechStackId == stackId, cancellationToken);

    public async Task DeleteAsync(TechStack stack, CancellationToken cancellationToken)
    {
        // Only the key is needed to delete; no FK or audit column is written here.
        _writeContext.TechStacks.Remove(new TechStackEntity { Id = stack.Id });
        await _writeContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TechStack> MapWithCategoryAsync(
        TechStackEntity entity, CancellationToken cancellationToken)
    {
        // The published shape flattens the category name, so load the navigation the
        // mapper reads. The writer has SELECT on the same tables it may modify.
        await _writeContext.Entry(entity)
            .Reference(item => item.TechStackCategory)
            .LoadAsync(cancellationToken);

        return TechStackMapper.ToDomain(entity);
    }
}
