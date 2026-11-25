using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Features.Experience;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Updates an existing project's experience link only. This covers attach, move and
/// detach in one route. The project's editable fields and tech stacks are not touched.
/// </summary>
public sealed class UpdateProjectExperienceHandler
    : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    private readonly IExperienceRepository _experiences;

    public UpdateProjectExperienceHandler(
        IProjectRepository projects,
        IExperienceRepository experiences,
        ILogger<UpdateProjectExperienceHandler> logger)
        : base(projects, logger) =>
        _experiences = experiences;

    public async Task<Result<ProjectDto>> HandleAsync(
        int projectId,
        UpdateProjectExperienceRequest? request,
        CancellationToken cancellationToken)
    {
        if (projectId <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A project id is required."));
        }

        if (request is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A request body is required."));
        }

        if (request.ExperienceId is not null && request.ExperienceId <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("Experience id must be positive when provided."));
        }

        if (request.ExperienceId is not null)
        {
            var expCheck = await ExecuteAsync(
                async ct => await _experiences.GetByIdAsync(request.ExperienceId!.Value, ct),
                "look up the experience",
                cancellationToken);
            if (!expCheck.IsSuccess)
            {
                return Result<ProjectDto>.Failure(expCheck.Error!);
            }
            if (expCheck.Value is null)
            {
                return Result<ProjectDto>.Failure(Error.NotFound("The experience does not exist."));
            }
        }

        var outcome = await ExecuteAsync<DomainProject?>(async ct =>
        {
            var project = await Repository.GetByIdAsync(projectId, ct);
            if (project is null)
            {
                return null;
            }

            project.AttachToExperience(request.ExperienceId);
            return await Repository.UpdateAsync(project, ct);
        }, "update a project's experience link", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }
}