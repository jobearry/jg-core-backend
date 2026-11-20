using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Microsoft.Extensions.Logging;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// The Project use case: choose the query, order the result, map to the slice's
/// DTO, and wrap failures in a typed <see cref="Result{T}"/>.
/// </summary>
public sealed class GetProjectHandler : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    public GetProjectHandler(IProjectRepository repository, ILogger<GetProjectHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<List<ProjectDto>>> HandleAsync(
        bool includeTools,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var projects = includeTools
                ? await Repository.GetAllWithTechStacksAsync(ct)
                : await Repository.GetAllAsync(ct);

            return projects
                .OrderBy(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio project content", cancellationToken);
    }
}
