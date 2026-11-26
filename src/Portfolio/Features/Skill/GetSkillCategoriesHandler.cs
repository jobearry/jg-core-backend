using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// The public category collection use case. Categories are shared reference data, and
/// this reads them through the read-only credential like every other portfolio read.
/// </summary>
public sealed class GetSkillCategoriesHandler
    : PortfolioServiceBase<TechStackCategory, ITechStackCategoryRepository>
{
    public GetSkillCategoriesHandler(
        ITechStackCategoryRepository repository, ILogger<GetSkillCategoriesHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<List<SkillCategoryDto>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var categories = await Repository.GetAllAsync(ct);

            return categories
                .OrderBy(category => category.Id)
                .Select(category => new SkillCategoryDto(category.Id, category.Category))
                .ToList();
        }, "get portfolio skill categories", cancellationToken);
    }
}
