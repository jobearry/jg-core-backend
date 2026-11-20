using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioExperienceEndpoints
{
    public static void MapPortfolioExperienceEndpoints(this IEndpointRouteBuilder routes)
    {
        // The CancellationToken is bound by the framework to RequestAborted. Passing it
        // all the way down means a client that navigates away stops the query instead of
        // holding a connection until it finishes.
        routes.MapGet("experience", async (
            [FromServices] PortfolioExperienceService expService,
            CancellationToken cancellationToken,
            bool includeProjects = false) =>
            (await expService.GetAllExperienceAsync(includeProjects, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioExperience")
        .WithTags("Portfolio")
        .Produces<List<ExperienceDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
