using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Persistence.Repositories;

public class EfPortfolioRecommendationRepository : BasePortfolioRepository<Recommendation, RecommendationEntity>, IPortfolioRecommendationRepository
{
    public EfPortfolioRecommendationRepository(PortfolioContext context)
        : base(context, RecommendationMapper.ToDomain) { }

    public async Task<List<Recommendation>> GetAllRecommendationsAsync(CancellationToken cancellationToken)
    {
        var recommendationEntities = await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        return recommendationEntities.Select(RecommendationMapper.ToDomain).ToList();
    }
}
