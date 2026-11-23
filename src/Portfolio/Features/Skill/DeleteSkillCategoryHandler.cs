using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// Deletes a category only when no stack still points at it.
/// </summary>
public sealed class DeleteSkillCategoryHandler
    : PortfolioServiceBase<TechStackCategory, ITechStackCategoryRepository>
{
    public DeleteSkillCategoryHandler(
        ITechStackCategoryRepository repository, ILogger<DeleteSkillCategoryHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<int>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<int>.Failure(Error.Validation("A tech stack category id is required."));
        }

        var lookup = await ExecuteAsync(
            ct => Repository.GetByIdAsync(id, ct), "get a tech stack category", cancellationToken);
        if (!lookup.IsSuccess)
        {
            return Result<int>.Failure(lookup.Error!);
        }

        if (lookup.Value is null)
        {
            return Result<int>.Failure(Error.NotFound("The category does not exist."));
        }

        var inUse = await ExecuteAsync(
            ct => Repository.HasStacksAsync(id, ct),
            "check category usage", cancellationToken);
        if (!inUse.IsSuccess)
        {
            return Result<int>.Failure(inUse.Error!);
        }

        if (inUse.Value)
        {
            return Result<int>.Failure(Error.Conflict("The category still has tech stacks."));
        }

        return await ExecuteAsync(async ct =>
        {
            await Repository.DeleteAsync(lookup.Value, ct);
            return id;
        }, "delete a tech stack category", cancellationToken);
    }
}
