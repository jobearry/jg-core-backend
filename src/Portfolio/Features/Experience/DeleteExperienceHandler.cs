using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// Deletes a role after a typed pre-write lookup, so a missing id is 404 rather than
/// a successful no-op. The database sets project.experience_id to null, leaving the
/// projects as standalone rows (ADR-0003).
/// </summary>
public sealed class DeleteExperienceHandler
    : PortfolioServiceBase<DomainExperience, IExperienceRepository>
{
    public DeleteExperienceHandler(
        IExperienceRepository repository, ILogger<DeleteExperienceHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<int>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<int>.Failure(Error.Validation("An experience id is required."));
        }

        var lookup = await ExecuteAsync(
            ct => Repository.GetByIdAsync(id, ct), "get an experience", cancellationToken);
        if (!lookup.IsSuccess)
        {
            return Result<int>.Failure(lookup.Error!);
        }

        if (lookup.Value is null)
        {
            return Result<int>.Failure(Error.NotFound("The experience does not exist."));
        }

        return await ExecuteAsync(async ct =>
        {
            await Repository.DeleteAsync(lookup.Value, ct);
            return id;
        }, "delete an experience", cancellationToken);
    }
}
