using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Skill;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Adds a single tech stack link to an existing project. The stack id is resolved through
/// the Skill read port; the domain replaces the whole set internally. The join belongs
/// to Project (ADR-0003) but this route provides an incremental alternative to replace-all.
/// </summary>
public sealed class AddProjectTechStackHandler
    : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    private readonly ISkillRepository _skills;

    public AddProjectTechStackHandler(
        IProjectRepository repository,
        ISkillRepository skills,
        ILogger<AddProjectTechStackHandler> logger)
        : base(repository, logger) =>
        _skills = skills;

    public async Task<Result<ProjectDto>> HandleAsync(
        int projectId,
        AddProjectTechStackRequest? request,
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

        var stackLookup = await ExecuteAsync(
            ct => _skills.GetByIdAsync(request.TechStackId, ct),
            "look up the tech stack",
            cancellationToken);
        if (!stackLookup.IsSuccess)
        {
            return Result<ProjectDto>.Failure(stackLookup.Error!);
        }
        if (stackLookup.Value is null)
        {
            return Result<ProjectDto>.Failure(Error.NotFound("The tech stack does not exist."));
        }

        var stack = stackLookup.Value;
        var stackRef = new TechStackRef(stack.Id, stack.StackName, stack.ImgSrc, stack.Category);

        // Check if already linked before the write
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
        if (projectCheck.Value.TechStacks.Any(r => r.Id == stackRef.Id))
        {
            return Result<ProjectDto>.Failure(Error.Validation("The tech stack is already linked to this project."));
        }

        var outcome = await ExecuteAsync<DomainProject?>(async ct =>
        {
            var project = await Repository.GetByIdAsync(projectId, ct);
            if (project is null)
            {
                return null;
            }

            var newRefs = project.TechStacks.Append(stackRef).ToList();
            project.ReplaceTechStacks(newRefs);
            return await Repository.AddTechStackAsync(project, stackRef, ct);
        }, "add a tech stack link", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }
}