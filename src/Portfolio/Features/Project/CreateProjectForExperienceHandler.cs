using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Features.Experience;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Creates a project under an existing experience. The parent is checked through its
/// own repository port; the write itself stays in the Project aggregate and uses
/// <see cref="DomainProject.AttachToExperience"/> rather than setting the link directly.
/// The tech-stack refs stay empty because that join has its own operation (ADR-0003).
/// </summary>
public sealed class CreateProjectForExperienceHandler
    : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    private readonly IExperienceRepository _experiences;

    public CreateProjectForExperienceHandler(
        IProjectRepository projects,
        IExperienceRepository experiences,
        ILogger<CreateProjectForExperienceHandler> logger)
        : base(projects, logger) =>
        _experiences = experiences;

    public async Task<Result<ProjectDto>> HandleAsync(
        int experienceId,
        CreateProjectRequest? request,
        CancellationToken cancellationToken)
    {
        if (experienceId <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("An experience id is required."));
        }

        if (request is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = ProjectRequestValidator.Validate(request.ProjectName);
        if (validationError is not null)
        {
            return Result<ProjectDto>.Failure(validationError);
        }

        var outcome = await ExecuteAsync<DomainProject?>(async ct =>
        {
            if (await _experiences.GetByIdAsync(experienceId, ct) is null)
            {
                return null;
            }

            var project = new DomainProject(
                0,
                null,
                request.ProjectName,
                request.Description,
                request.Contribution);
            project.AttachToExperience(experienceId);

            return await Repository.AddAsync(project, ct);
        }, "create a project for an experience", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The experience does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }
}
