using Portfolio.Domain.Interfaces;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// The Recommendation aggregate's repository port. Reads use the read-only context;
/// the write methods use the separate DML credential inside the EF adapter, so the
/// public read path never needs write rights.
/// </summary>
public interface IRecommendationRepository : IRepository<DomainRecommendation>
{
    Task<DomainRecommendation?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<DomainRecommendation> AddAsync(
        DomainRecommendation recommendation,
        CancellationToken cancellationToken);

    Task<DomainRecommendation> UpdateAsync(
        DomainRecommendation recommendation,
        CancellationToken cancellationToken);

    Task DeleteAsync(DomainRecommendation recommendation, CancellationToken cancellationToken);
}
