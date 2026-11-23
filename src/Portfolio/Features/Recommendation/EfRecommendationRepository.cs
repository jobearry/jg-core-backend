using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// EF adapter for the Recommendation port. The base read uses the read-only context;
/// admin writes use <see cref="PortfolioWriteContext"/> so the public query never
/// carries the writer credential.
/// </summary>
public sealed class EfRecommendationRepository
    : BasePortfolioRepository<DomainRecommendation, RecommendationEntity>, IRecommendationRepository
{
    private readonly PortfolioWriteContext _writeContext;

    public EfRecommendationRepository(
        PortfolioContext context,
        PortfolioWriteContext writeContext)
        : base(context, RecommendationMapper.ToDomain) =>
        _writeContext = writeContext;

    public async Task<DomainRecommendation?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        // Public detail reads and admin pre-write lookups both use the read-only
        // context; writes below re-attach the mapped entity to _writeContext.
        var entity = await _dbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return entity is null ? null : RecommendationMapper.ToDomain(entity);
    }

    public async Task<DomainRecommendation> AddAsync(
        DomainRecommendation recommendation,
        CancellationToken cancellationToken)
    {
        var entity = RecommendationMapper.ToEntity(recommendation);

        // Identity-always generates the id; the domain aggregate is rebuilt from the
        // stored row so its get-only Id reflects the database.
        _writeContext.Recommendations.Add(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return RecommendationMapper.ToDomain(entity);
    }

    public async Task<DomainRecommendation> UpdateAsync(
        DomainRecommendation recommendation,
        CancellationToken cancellationToken)
    {
        var entity = RecommendationMapper.ToEntity(recommendation);

        _writeContext.Recommendations.Update(entity);
        await _writeContext.SaveChangesAsync(cancellationToken);

        return RecommendationMapper.ToDomain(entity);
    }

    public async Task DeleteAsync(
        DomainRecommendation recommendation,
        CancellationToken cancellationToken)
    {
        _writeContext.Recommendations.Remove(RecommendationMapper.ToEntity(recommendation));
        await _writeContext.SaveChangesAsync(cancellationToken);
    }
}
