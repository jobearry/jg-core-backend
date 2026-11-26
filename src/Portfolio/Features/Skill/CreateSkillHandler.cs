using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// Creates a stack from an existing category name. The name is resolved to an id before
/// the insert so the public read path never needs write rights on the category table.
/// </summary>
public sealed class CreateSkillHandler : PortfolioServiceBase<TechStack, ISkillRepository>
{
    private readonly ITechStackCategoryRepository _categories;

    public CreateSkillHandler(
        ISkillRepository stacks,
        ITechStackCategoryRepository categories,
        ILogger<CreateSkillHandler> logger)
        : base(stacks, logger) =>
        _categories = categories;

    public async Task<Result<TechStackDto>> HandleAsync(
        CreateSkillRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<TechStackDto>.Failure(Error.Validation("A request body is required."));
        }

        var validationError = SkillRequestValidator.ValidateStack(
            request.StackName, request.ImgSrc, request.Category);
        if (validationError is not null)
        {
            return Result<TechStackDto>.Failure(validationError);
        }

        var lookup = await ExecuteAsync(
            ct => _categories.GetByNameAsync(request.Category, ct),
            "resolve a tech stack category", cancellationToken);
        if (!lookup.IsSuccess)
        {
            return Result<TechStackDto>.Failure(lookup.Error!);
        }

        if (lookup.Value is null)
        {
            return Result<TechStackDto>.Failure(Error.Validation("The category does not exist."));
        }

        var category = lookup.Value;
        return await ExecuteAsync(async ct =>
        {
            var created = await Repository.AddAsync(
                new TechStack(0, request.StackName, request.ImgSrc, category.Category),
                category.Id, ct);

            return created.ToDto();
        }, "create a tech stack", cancellationToken);
    }
}
