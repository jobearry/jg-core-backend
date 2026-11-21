using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Microsoft.Extensions.Logging;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// The Recommendation use case: order the rows, map to the slice's DTO and turn
/// failures into typed <see cref="Result{T}"/> values.
/// </summary>
public sealed class GetRecommendationHandler
    : PortfolioServiceBase<DomainRecommendation, IRecommendationRepository>
{
    public GetRecommendationHandler(
        IRecommendationRepository repository,
        ILogger<GetRecommendationHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<List<RecommendationDto>>> HandleAsync(CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var recommendations = await Repository.GetAllAsync(ct);

            return recommendations
                .OrderBy(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio recommendation content", cancellationToken);
    }
}
