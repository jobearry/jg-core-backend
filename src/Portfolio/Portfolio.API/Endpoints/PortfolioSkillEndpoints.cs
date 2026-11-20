using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Endpoints;

public static class PortfolioSkillEndpoints
{
    public static void MapPortfolioSkillEndpoints(this IEndpointRouteBuilder routes)
    {
        // This endpoint used to ignore IsSuccess and return 200 with a null body on
        // failure, which a client could not distinguish from "no skills exist". It goes
        // through the same mapper as its siblings now.
        routes.MapGet("skill", async (
            [FromServices] PortfolioSkillService skillService,
            CancellationToken cancellationToken) =>
            (await skillService.GetAllTechStacksAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioSkills")
        .WithTags("Portfolio")
        .Produces<List<TechStackDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
