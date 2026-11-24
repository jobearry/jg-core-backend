using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// The anonymous detail use case, so a created role's Location is followable and
/// returns the same DTO shape as the collection with includeProjects=true.
/// </summary>
public sealed class GetExperienceByIdHandler
    : PortfolioServiceBase<DomainExperience, IExperienceRepository>
{
    public GetExperienceByIdHandler(
        IExperienceRepository repository, ILogger<GetExperienceByIdHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ExperienceDto>> HandleAsync(
        int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<ExperienceDto>.Failure(Error.Validation("An experience id is required."));
        }

        var outcome = await ExecuteAsync<DomainExperience?>(
            ct => Repository.GetByIdAsync(id, ct), "get an experience", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ExperienceDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ExperienceDto>.Failure(Error.NotFound("The experience does not exist."))
            : Result<ExperienceDto>.Success(outcome.Value.ToDto());
    }
}
