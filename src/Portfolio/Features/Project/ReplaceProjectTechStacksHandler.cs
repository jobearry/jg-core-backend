using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Skill;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Replaces a project's tech stacks from shared stack ids. The Skill read port supplies
/// each snapshot, so the client cannot forge a name, image or category. Project owns the
/// join write through its own repository (ADR-0003).
/// </summary>
public sealed class ReplaceProjectTechStacksHandler
    : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    private readonly ISkillRepository _skills;

    public ReplaceProjectTechStacksHandler(
        IProjectRepository repository,
        ISkillRepository skills,
        ILogger<ReplaceProjectTechStacksHandler> logger)
        : base(repository, logger) => _skills = skills;

    public async Task<Result<ProjectDto>> HandleAsync(
        int id, ReplaceProjectTechStacksRequest? request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A project id is required."));
        }

        if (request is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A request body is required."));
        }

        // The validator still rejects null at runtime; the suppression is only so the
        // compiler knows the list is non-null inside the lambdas below.
        var stackIds = request.TechStackIds!;
        var validationError = ProjectTechStackRequestValidator.Validate(stackIds);
        if (validationError is not null)
        {
            return Result<ProjectDto>.Failure(validationError);
        }

        // Resolve sequentially: the Skill port shares one EF context, which cannot run
        // two queries at once. Any lookup failure is classified by the shared base.
        var resolution = await ExecuteAsync<StackResolution>(async ct =>
        {
            var refs = new List<TechStackRef>();
            foreach (var stackId in stackIds)
            {
                var stack = await _skills.GetByIdAsync(stackId, ct);
                if (stack is not null)
                {
                    refs.Add(new TechStackRef(
                        stack.Id, stack.StackName, stack.ImgSrc, stack.Category));
                }
            }

            return new StackResolution(refs, refs.Count != stackIds.Count);
        }, "resolve tech stacks", cancellationToken);

        if (!resolution.IsSuccess)
        {
            return Result<ProjectDto>.Failure(resolution.Error!);
        }

        if (resolution.Value!.MissingStack)
        {
            return Result<ProjectDto>.Failure(
                Error.Validation("One or more tech stack ids do not exist."));
        }

        var refs = resolution.Value.Refs;
        var outcome = await ExecuteAsync<DomainProject?>(async ct =>
        {
            var project = await Repository.GetByIdAsync(id, ct);
            if (project is null)
            {
                return null;
            }

            project.ReplaceTechStacks(refs);
            return await Repository.ReplaceTechStacksAsync(project, ct);
        }, "replace project tech stacks", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }

    /// <summary>Refs plus whether any requested id had no matching shared row.</summary>
    private sealed record StackResolution(List<TechStackRef> Refs, bool MissingStack);
}
