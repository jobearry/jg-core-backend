using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.ValueObjects;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// Adds a role with all of its scalar fields. The Projects projection stays empty:
/// projects own their own experience_id and are attached by the Project slice.
/// </summary>
public sealed class CreateExperienceHandler
    : PortfolioServiceBase<DomainExperience, IExperienceRepository>
{
    public CreateExperienceHandler(
        IExperienceRepository repository, ILogger<CreateExperienceHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ExperienceDto>> HandleAsync(
        CreateExperienceRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<ExperienceDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = ExperienceRequestValidator.Validate(
            request.Company, request.StartedAt, request.FinishedAt, request.Role, request.Type);
        if (validationError is not null)
        {
            return Result<ExperienceDto>.Failure(validationError);
        }

        return await ExecuteAsync(async ct =>
        {
            var experience = new DomainExperience(
                0,
                new Company(request.Company),
                new Period(request.StartedAt, request.FinishedAt),
                ExperienceRequestValidator.ToRole(request.Role));
            experience.UpdateDescription(request.Description);
            experience.UpdateResponsibility(request.Responsibility);
            experience.UpdateType(request.Type);

            var created = await Repository.AddAsync(experience, ct);
            return created.ToDto();
        }, "create an experience", cancellationToken);
    }
}
