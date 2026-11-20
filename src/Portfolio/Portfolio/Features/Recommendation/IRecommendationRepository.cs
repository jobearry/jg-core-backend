using Portfolio.Domain.Interfaces;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// The Recommendation slice's port. As with Skill, the old interface only
/// restated <see cref="IRepository{T}.GetAllAsync"/>, so the slice uses that.
/// </summary>
public interface IRecommendationRepository : IRepository<DomainRecommendation>;
