using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Replaces the project's editable fields through the domain method. The identity,
/// experience link and stack refs are not part of this command.
/// </summary>
public sealed class UpdateProjectHandler : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    public UpdateProjectHandler(IProjectRepository repository, ILogger<UpdateProjectHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ProjectDto>> HandleAsync(
        int id, UpdateProjectRequest? request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A project id is required."));
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
            var project = await Repository.GetByIdAsync(id, ct);
            if (project is null)
            {
                return null;
            }

            project.UpdateDetails(
                request.ProjectName, request.Description, request.Contribution);
            return await Repository.UpdateAsync(project, ct);
        }, "update a project", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }
}
