using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Removes a single tech stack link from an existing project. The join belongs
/// to Project (ADR-0003) but this route provides an incremental alternative to replace-all.
/// </summary>
public sealed class RemoveProjectTechStackHandler
    : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    public RemoveProjectTechStackHandler(
        IProjectRepository repository,
        ILogger<RemoveProjectTechStackHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ProjectDto>> HandleAsync(
        int projectId,
        RemoveProjectTechStackRequest? request,
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

        if (request.TechStackId <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("Tech stack id must be positive."));
        }

        // Check if project exists and if the stack is linked before the write
        var projectCheck = await ExecuteAsync(
            ct => Repository.GetByIdAsync(projectId, ct),
            "look up the project",
            cancellationToken);
        if (!projectCheck.IsSuccess)
        {
            return Result<ProjectDto>.Failure(projectCheck.Error!);
        }
        if (projectCheck.Value is null)
        {
            return Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."));
        }
        if (!projectCheck.Value.TechStacks.Any(r => r.Id == request.TechStackId))
        {
            return Result<ProjectDto>.Failure(Error.NotFound("The tech stack is not linked to this project."));
        }

        var outcome = await ExecuteAsync<DomainProject?>(async ct =>
        {
            var project = await Repository.GetByIdAsync(projectId, ct);
            if (project is null)
            {
                return null;
            }

            var newRefs = project.TechStacks.Where(r => r.Id != request.TechStackId).ToList();
            project.ReplaceTechStacks(newRefs);
            return await Repository.RemoveTechStackAsync(project, request.TechStackId, ct);
        }, "remove a tech stack link", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }
}