using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioRecommendationService : PortfolioServiceBase<Recommendation, IPortfolioRecommendationRepository>
{
    public PortfolioRecommendationService(IPortfolioRecommendationRepository repository)
        : base(repository) { }

    public async Task<Result<List<Recommendation>>> GetAllRecommendationsAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var recommendations = await Repository.GetAllRecommendationsAsync();

            return recommendations.OrderBy(x => x.Id).ToList();
        }, "get portfolio recommendation content");
    }
}
