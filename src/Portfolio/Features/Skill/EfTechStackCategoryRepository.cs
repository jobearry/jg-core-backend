using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;

namespace Portfolio.Features.Skill;

/// <summary>
/// EF adapter for categories. Reads use the read-only context; admin writes use the
/// DML credential. Category names have no unique constraint, so a name lookup picks
/// the lowest id rather than failing on duplicates.
/// </summary>
public sealed class EfTechStackCategoryRepository
    : BasePortfolioRepository<TechStackCategory, TechStackCategoryEntity>, ITechStackCategoryRepository
{
    private readonly PortfolioWriteContext _writeContext;

    public EfTechStackCategoryRepository(PortfolioContext context, PortfolioWriteContext writeContext)
        : base(context, TechStackCategoryMapper.ToDomain) =>
        _writeContext = writeContext;

    public async Task<TechStackCategory?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

        return entity is null ? null : TechStackCategoryMapper.ToDomain(entity);
    }

    public async Task<TechStackCategory?> GetByNameAsync(
        string category, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.AsNoTracking()
            .Where(item => item.Category == category)
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : TechStackCategoryMapper.ToDomain(entity);
    }

    public async Task<TechStackCategory> AddAsync(
        TechStackCategory category, CancellationToken cancellationToken)
    {
        var entity = TechStackCategoryMapper.ToEntity(category);

        _writeContext.TechStackCategories.Add(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return TechStackCategoryMapper.ToDomain(entity);
    }

    public async Task<TechStackCategory> UpdateAsync(
        TechStackCategory category, CancellationToken cancellationToken)
    {
        var entity = TechStackCategoryMapper.ToEntity(category);

        // Update() would mark created_at modified and clear the database default.
        _writeContext.TechStackCategories.Attach(entity);
        _writeContext.Entry(entity).Property(e => e.Category).IsModified = true;
        await _writeContext.SaveChangesAsync(cancellationToken);

        return TechStackCategoryMapper.ToDomain(entity);
    }

    public Task<bool> HasStacksAsync(int categoryId, CancellationToken cancellationToken) =>
        _context.Set<TechStackEntity>()
            .AnyAsync(stack => stack.TechStackCategoryId == categoryId, cancellationToken);

    public async Task DeleteAsync(TechStackCategory category, CancellationToken cancellationToken)
    {
        _writeContext.TechStackCategories.Remove(new TechStackCategoryEntity { Id = category.Id });
        await _writeContext.SaveChangesAsync(cancellationToken);
    }
}
