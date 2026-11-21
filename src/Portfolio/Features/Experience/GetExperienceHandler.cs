using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Microsoft.Extensions.Logging;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience;

/// <summary>
/// The Experience use case. It selects the query, orders the result and maps it to
/// the slice's DTO; every failure is turned into a typed <see cref="Result{T}"/> by
/// the shared base rather than escaping as an exception.
/// </summary>
public sealed class GetExperienceHandler
    : PortfolioServiceBase<DomainExperience, IExperienceRepository>
{
    public GetExperienceHandler(IExperienceRepository repository, ILogger<GetExperienceHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<List<ExperienceDto>>> HandleAsync(
        bool includeProjects,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var experiences = includeProjects
                ? await Repository.GetAllWithProjectsAsync(ct)
                : await Repository.GetAllAsync(ct);

            return experiences
                .OrderByDescending(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio experience content", cancellationToken);
    }
}
