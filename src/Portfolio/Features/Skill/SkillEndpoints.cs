using Microsoft.AspNetCore.Authorization;
using Portfolio.Endpoints;

namespace Portfolio.Features.Skill;

public static class SkillEndpoints
{
    public static void MapSkillEndpoints(this IEndpointRouteBuilder routes)
    {
        // This endpoint used to ignore IsSuccess and return 200 with a null body on
        // failure, which a client could not distinguish from "no skills exist".
        routes.MapGet("skill", async (
                GetSkillHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioSkills")
        .WithTags("Portfolio")
        .AllowAnonymous()
        .Produces<List<TechStackDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
