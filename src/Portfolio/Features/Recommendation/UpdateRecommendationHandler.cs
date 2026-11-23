using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.ValueObjects;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// Replaces the editable fields of one testimonial. <c>PostedAt</c> stays with the
/// original row; an edit does not change when the testimonial was published.
/// </summary>
public sealed class UpdateRecommendationHandler
    : PortfolioServiceBase<DomainRecommendation, IRecommendationRepository>
{
    public UpdateRecommendationHandler(
        IRecommendationRepository repository,
        ILogger<UpdateRecommendationHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<RecommendationDto>> HandleAsync(
        int id,
        UpdateRecommendationRequest? request,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<RecommendationDto>.Failure(Error.Validation("A recommendation id is required."));
        }

        if (request is null)
        {
            return Result<RecommendationDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = RecommendationRequestValidator.Validate(
            request.PersonName, request.Description, request.CareerPosition);

        if (validationError is not null)
        {
            return Result<RecommendationDto>.Failure(validationError);
        }

        var outcome = await ExecuteAsync<DomainRecommendation?>(async ct =>
        {
            var existing = await Repository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return null;
            }

            var careerPosition = request.CareerPosition is null
                ? null
                : new Role(request.CareerPosition);

            existing.ChangeDetails(request.PersonName, request.Description, careerPosition);
            return await Repository.UpdateAsync(existing, ct);
        }, "update a recommendation", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<RecommendationDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<RecommendationDto>.Failure(Error.NotFound("The recommendation does not exist."))
            : Result<RecommendationDto>.Success(outcome.Value.ToDto());
    }
}
