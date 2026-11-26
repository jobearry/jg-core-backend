using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project;

/// <summary>
/// Deletes a project after a typed pre-write lookup, so a missing id is 404 rather
/// than a successful no-op. The database cascades the join rows it owns.
/// </summary>
public sealed class DeleteProjectHandler : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    public DeleteProjectHandler(IProjectRepository repository, ILogger<DeleteProjectHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<int>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<int>.Failure(Error.Validation("A project id is required."));
        }

        var lookup = await ExecuteAsync(
            ct => Repository.GetByIdAsync(id, ct), "get a project", cancellationToken);
        if (!lookup.IsSuccess)
        {
            return Result<int>.Failure(lookup.Error!);
        }

        if (lookup.Value is null)
        {
            return Result<int>.Failure(Error.NotFound("The project does not exist."));
        }

        return await ExecuteAsync(async ct =>
        {
            await Repository.DeleteAsync(lookup.Value, ct);
            return id;
        }, "delete a project", cancellationToken);
    }
}
