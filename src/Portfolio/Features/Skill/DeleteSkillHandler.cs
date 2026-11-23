using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// Deletes a stack only when no project cites it. Removing a cited stack would fail at
/// the join-table foreign key, so the guard turns that into a typed conflict.
/// </summary>
public sealed class DeleteSkillHandler : PortfolioServiceBase<TechStack, ISkillRepository>
{
    public DeleteSkillHandler(ISkillRepository repository, ILogger<DeleteSkillHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<int>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<int>.Failure(Error.Validation("A tech stack id is required."));
        }

        var lookup = await ExecuteAsync(
            ct => Repository.GetByIdAsync(id, ct), "get a tech stack", cancellationToken);
        if (!lookup.IsSuccess)
        {
            return Result<int>.Failure(lookup.Error!);
        }

        if (lookup.Value is null)
        {
            return Result<int>.Failure(Error.NotFound("The tech stack does not exist."));
        }

        var inUse = await ExecuteAsync(
            ct => Repository.IsUsedByProjectsAsync(id, ct),
            "check tech stack usage", cancellationToken);
        if (!inUse.IsSuccess)
        {
            return Result<int>.Failure(inUse.Error!);
        }

        if (inUse.Value)
        {
            return Result<int>.Failure(Error.Conflict("The tech stack is used by projects."));
        }

        return await ExecuteAsync(async ct =>
        {
            await Repository.DeleteAsync(lookup.Value, ct);
            return id;
        }, "delete a tech stack", cancellationToken);
    }
}
