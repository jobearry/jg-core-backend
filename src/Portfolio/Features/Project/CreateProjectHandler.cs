using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Creates a standalone project. The experience link is left null and no tech-stack
/// rows are written; both belong to their own operations (ADR-0003).
/// </summary>
public sealed class CreateProjectHandler : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    public CreateProjectHandler(IProjectRepository repository, ILogger<CreateProjectHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ProjectDto>> HandleAsync(
        CreateProjectRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = ProjectRequestValidator.Validate(request.ProjectName);
        if (validationError is not null)
        {
            return Result<ProjectDto>.Failure(validationError);
        }

        return await ExecuteAsync(async ct =>
        {
            var created = await Repository.AddAsync(
                new DomainProject(
                    0,
                    null,
                    request.ProjectName,
                    request.Description,
                    request.Contribution),
                ct);

            return created.ToDto();
        }, "create a project", cancellationToken);
    }
}
