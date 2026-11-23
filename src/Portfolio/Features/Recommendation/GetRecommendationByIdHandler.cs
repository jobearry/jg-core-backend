using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// The public detail use case. A missing row is a typed not-found failure so the
/// endpoint can answer 404 rather than returning an empty body with 200.
/// </summary>
public sealed class GetRecommendationByIdHandler
    : PortfolioServiceBase<DomainRecommendation, IRecommendationRepository>
{
    public GetRecommendationByIdHandler(
        IRecommendationRepository repository,
        ILogger<GetRecommendationByIdHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<RecommendationDto>> HandleAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<RecommendationDto>.Failure(Error.Validation("A recommendation id is required."));
        }

        var outcome = await ExecuteAsync<DomainRecommendation?>(async ct =>
            await Repository.GetByIdAsync(id, ct), "get a recommendation", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<RecommendationDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<RecommendationDto>.Failure(Error.NotFound("The recommendation does not exist."))
            : Result<RecommendationDto>.Success(outcome.Value.ToDto());
    }
}
