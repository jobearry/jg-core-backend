using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Application.Services;

public class PortfolioRecommendationService : PortfolioServiceBase<Recommendation, IPortfolioRecommendationRepository>
{
    public PortfolioRecommendationService(IPortfolioRecommendationRepository repository, ILogger<PortfolioRecommendationService> logger)
        : base(repository, logger) { }

    public async Task<Result<List<RecommendationDto>>> GetAllRecommendationsAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var recommendations = await Repository.GetAllRecommendationsAsync(ct);

            return recommendations
                .OrderBy(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio recommendation content", cancellationToken);
    }
}
