using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// The anonymous detail use case, so a created project's Location is followable and
/// returns the same DTO shape as the collection with tools included.
/// </summary>
public sealed class GetProjectByIdHandler : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    public GetProjectByIdHandler(IProjectRepository repository, ILogger<GetProjectByIdHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<ProjectDto>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<ProjectDto>.Failure(Error.Validation("A project id is required."));
        }

        var outcome = await ExecuteAsync<DomainProject?>(
            ct => Repository.GetByIdAsync(id, ct), "get a project", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<ProjectDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<ProjectDto>.Failure(Error.NotFound("The project does not exist."))
            : Result<ProjectDto>.Success(outcome.Value.ToDto());
    }
}
