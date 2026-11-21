using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;
using Portfolio.Persistence.Repositories;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// EF adapter for the Recommendation port. The base read needs no includes: the
/// mapper flattens the nullable career position straight from the row.
/// </summary>
public sealed class EfRecommendationRepository
    : BasePortfolioRepository<DomainRecommendation, RecommendationEntity>, IRecommendationRepository
{
    public EfRecommendationRepository(PortfolioContext context)
        : base(context, RecommendationMapper.ToDomain) { }
}
