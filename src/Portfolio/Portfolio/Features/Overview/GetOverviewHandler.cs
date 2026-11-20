using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Features.Project;
using Portfolio.Features.Recommendation;
using Portfolio.Features.Skill;
using Microsoft.Extensions.Logging;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Overview;

/// <summary>
/// The Overview use case. It reads only the counts the client needs from the three
/// slice ports; the order is deliberate because the repositories share one EF context.
/// </summary>
public sealed class GetOverviewHandler : PortfolioServiceBase<DomainProject, IProjectRepository>
{
    private readonly ISkillRepository _skills;
    private readonly IRecommendationRepository _recommendations;

    public GetOverviewHandler(
        IProjectRepository projects,
        ISkillRepository skills,
        IRecommendationRepository recommendations,
        ILogger<GetOverviewHandler> logger)
        : base(projects, logger)
    {
        _skills = skills;
        _recommendations = recommendations;
    }

    public async Task<Result<List<OverviewItemDto>>> HandleAsync(CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            // All three repositories share the scoped PortfolioContext, and EF Core
            // forbids concurrent operations on one context. Task.WhenAll here throws
            // "A second operation was started on this context instance". Parallelism
            // needs a context per query, which costs more than it saves for three counts.
            var projects = await Repository.GetAllWithTechStacksAsync(ct);
            var skills = await _skills.GetAllAsync(ct);
            var recommendations = await _recommendations.GetAllAsync(ct);

            // Keys are the contract; the client owns labels, icons and order. Only add a
            // key here when the client has a tile for it.
            return new List<OverviewItemDto>
            {
                new("projects", projects.Count),
                new("skills", skills.Count),
                new("recommendations", recommendations.Count)
            };
        }, "build the dashboard overview", cancellationToken);
    }
}
