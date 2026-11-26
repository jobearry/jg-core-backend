using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// Renames a category through the domain method, so the name invariant runs on edits.
/// </summary>
public sealed class UpdateSkillCategoryHandler
    : PortfolioServiceBase<TechStackCategory, ITechStackCategoryRepository>
{
    public UpdateSkillCategoryHandler(
        ITechStackCategoryRepository repository, ILogger<UpdateSkillCategoryHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<SkillCategoryDto>> HandleAsync(
        int id, SkillCategoryRequest? request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<SkillCategoryDto>.Failure(
                Error.Validation("A tech stack category id is required."));
        }

        if (request is null)
        {
            return Result<SkillCategoryDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = SkillRequestValidator.ValidateCategory(request.Category);
        if (validationError is not null)
        {
            return Result<SkillCategoryDto>.Failure(validationError);
        }

        var outcome = await ExecuteAsync<TechStackCategory?>(async ct =>
        {
            var category = await Repository.GetByIdAsync(id, ct);
            if (category is null)
            {
                return null;
            }

            category.Rename(request.Category);
            return await Repository.UpdateAsync(category, ct);
        }, "update a tech stack category", cancellationToken);

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
