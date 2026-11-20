using Portfolio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Domain.Interfaces;

public interface IPortfolioRecommendationRepository : IRepository<Recommendation>
{
    Task<List<Recommendation>> GetAllRecommendationsAsync(CancellationToken cancellationToken);
}
