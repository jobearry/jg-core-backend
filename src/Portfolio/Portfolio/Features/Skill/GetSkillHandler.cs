using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Portfolio.Features.Skill;

/// <summary>
/// The Skill use case. Every failure becomes a typed <see cref="Result{T}"/> so the
/// endpoint cannot answer 200 with a null body when the database is down.
/// </summary>
public sealed class GetSkillHandler : PortfolioServiceBase<TechStack, ISkillRepository>
{
    public GetSkillHandler(ISkillRepository repository, ILogger<GetSkillHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<List<TechStackDto>>> HandleAsync(CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var techStacks = await Repository.GetAllAsync(ct);

            return techStacks
                .OrderBy(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio tech stack content", cancellationToken);
    }
}
