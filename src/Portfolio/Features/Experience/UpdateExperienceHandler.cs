using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// Replaces a role's editable fields through the domain method, so the value-object
/// invariants run on edits as well as creation. The identity and the Projects
/// projection are not part of this command.
/// </summary>
public sealed class UpdateExperienceHandler
    : PortfolioServiceBase<DomainExperience, IExperienceRepository>
{
    public UpdateExperienceHandler(
        IExperienceRepository repository, ILogger<UpdateExperienceHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ExperienceDto>> HandleAsync(
        int id, UpdateExperienceRequest? request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<ExperienceDto>.Failure(Error.Validation("An experience id is required."));
        }

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

        var outcome = await ExecuteAsync<DomainExperience?>(async ct =>
        {
            var experience = await Repository.GetByIdAsync(id, ct);
            if (experience is null)
            {
                return null;
            }

            experience.UpdateDetails(
                request.Company,
                request.StartedAt,
                request.FinishedAt,
                request.Role,
                request.Description,
                request.Responsibility,
                request.Type);
            return await Repository.UpdateAsync(experience, ct);
        }, "update an experience", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ExperienceDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ExperienceDto>.Failure(Error.NotFound("The experience does not exist."))
            : Result<ExperienceDto>.Success(outcome.Value.ToDto());
    }
}
