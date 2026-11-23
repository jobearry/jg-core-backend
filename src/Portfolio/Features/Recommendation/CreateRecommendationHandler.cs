using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.ValueObjects;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// Creates a testimonial with a server-supplied publication time. The client never
/// chooses <c>PostedAt</c>, because a forged date would misrepresent the content.
/// </summary>
public sealed class CreateRecommendationHandler
    : PortfolioServiceBase<DomainRecommendation, IRecommendationRepository>
{
    private readonly TimeProvider _timeProvider;

    public CreateRecommendationHandler(
        IRecommendationRepository repository,
        ILogger<CreateRecommendationHandler> logger,
        TimeProvider timeProvider)
        : base(repository, logger) =>
        _timeProvider = timeProvider;

    public async Task<Result<RecommendationDto>> HandleAsync(
        CreateRecommendationRequest? request,
        CancellationToken cancellationToken)
    {
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

        return await ExecuteAsync(async ct =>
        {
            var careerPosition = request.CareerPosition is null
                ? null
                : new Role(request.CareerPosition);

            var recommendation = new DomainRecommendation(
                0,
                request.PersonName,
                request.Description,
                careerPosition,
                _timeProvider.GetUtcNow().UtcDateTime);

            var created = await Repository.AddAsync(recommendation, ct);
            return created.ToDto();
        }, "create a recommendation", cancellationToken);
    }
}
