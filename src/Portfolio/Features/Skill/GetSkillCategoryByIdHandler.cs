using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// The anonymous detail use case, so a created category's Location header is followable.
/// </summary>
public sealed class GetSkillCategoryByIdHandler
    : PortfolioServiceBase<TechStackCategory, ITechStackCategoryRepository>
{
    public GetSkillCategoryByIdHandler(
        ITechStackCategoryRepository repository, ILogger<GetSkillCategoryByIdHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<SkillCategoryDto>> HandleAsync(
        int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<SkillCategoryDto>.Failure(
                Error.Validation("A tech stack category id is required."));
        }

        var outcome = await ExecuteAsync<TechStackCategory?>(
            ct => Repository.GetByIdAsync(id, ct), "get a tech stack category", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<SkillCategoryDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<SkillCategoryDto>.Failure(Error.NotFound("The category does not exist."))
            : Result<SkillCategoryDto>.Success(
                new SkillCategoryDto(outcome.Value.Id, outcome.Value.Category));
    }
}
