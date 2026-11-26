using Microsoft.Extensions.Logging;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.Features.Skill;

/// <summary>
/// The anonymous detail use case, so a created stack's Location header is followable.
/// </summary>
public sealed class GetSkillByIdHandler : PortfolioServiceBase<TechStack, ISkillRepository>
{
    public GetSkillByIdHandler(ISkillRepository repository, ILogger<GetSkillByIdHandler> logger)
        : base(repository, logger) { }

    public async Task<Result<TechStackDto>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result<TechStackDto>.Failure(Error.Validation("A tech stack id is required."));
        }

        var outcome = await ExecuteAsync<TechStack?>(
            ct => Repository.GetByIdAsync(id, ct), "get a tech stack", cancellationToken);

        if (!outcome.IsSuccess)
        {
            return Result<TechStackDto>.Failure(outcome.Error!);
        }

        return outcome.Value is null
            ? Result<TechStackDto>.Failure(Error.NotFound("The tech stack does not exist."))
            : Result<TechStackDto>.Success(outcome.Value.ToDto());
    }
}
