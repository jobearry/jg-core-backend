using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// Adds a shared category. Its id is database-generated; no audit fields are published.
/// </summary>
public sealed class CreateSkillCategoryHandler
    : PortfolioServiceBase<TechStackCategory, ITechStackCategoryRepository>
{
    public CreateSkillCategoryHandler(
        ITechStackCategoryRepository repository, ILogger<CreateSkillCategoryHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<SkillCategoryDto>> HandleAsync(
        SkillCategoryRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<SkillCategoryDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = SkillRequestValidator.ValidateCategory(request.Category);
        if (validationError is not null)
        {
            return Result<SkillCategoryDto>.Failure(validationError);
        }

        return await ExecuteAsync(async ct =>
        {
            var created = await Repository.AddAsync(
                new TechStackCategory(0, request.Category), ct);

            return new SkillCategoryDto(created.Id, created.Category);
        }, "create a tech stack category", cancellationToken);
    }
}
