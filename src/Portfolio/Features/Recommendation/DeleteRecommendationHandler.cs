using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// Deletes one testimonial. A missing row is a typed not-found failure so the endpoint
/// can answer 404 instead of pretending the delete succeeded.
/// </summary>
public sealed class DeleteRecommendationHandler
    : PortfolioServiceBase<DomainRecommendation, IRecommendationRepository>
{
    public DeleteRecommendationHandler(
        IRecommendationRepository repository,
        ILogger<DeleteRecommendationHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<int>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<int>.Failure(Error.Validation("A recommendation id is required."));
        }

        var outcome = await ExecuteAsync(async ct =>
        {
            var existing = await Repository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return false;
            }

            await Repository.DeleteAsync(existing, ct);
            return true;
        }, "delete a recommendation", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<int>.Failure(outcome.Error!);
        }

        return outcome.Value
            ? Result<int>.Success(id)
            : Result<int>.Failure(Error.NotFound("The recommendation does not exist."));
    }
}
