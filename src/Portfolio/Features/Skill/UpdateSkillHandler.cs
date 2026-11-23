using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// Replaces a stack's editable fields. The category is a reference to another
/// aggregate, so it is resolved by name and written by id in one command.
/// </summary>
public sealed class UpdateSkillHandler : PortfolioServiceBase<TechStack, ISkillRepository>
{
    private readonly ITechStackCategoryRepository _categories;

    public UpdateSkillHandler(
        ISkillRepository stacks,
        ITechStackCategoryRepository categories,
        ILogger<UpdateSkillHandler> logger)
        : base(stacks, logger) =>
        _categories = categories;

    public async Task<Result<TechStackDto>> HandleAsync(
        int id, UpdateSkillRequest? request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<TechStackDto>.Failure(Error.Validation("A tech stack id is required."));
        }

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
        var outcome = await ExecuteAsync<TechStack?>(async ct =>
        {
            var stack = await Repository.GetByIdAsync(id, ct);
            if (stack is null)
            {
                return null;
            }

            stack.UpdateDetails(request.StackName, request.ImgSrc);
            stack.SetCategory(category.Category);
            return await Repository.UpdateAsync(stack, category.Id, ct);
        }, "update a tech stack", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<TechStackDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<TechStackDto>.Failure(Error.NotFound("The tech stack does not exist."))
            : Result<TechStackDto>.Success(outcome.Value.ToDto());
    }
}
