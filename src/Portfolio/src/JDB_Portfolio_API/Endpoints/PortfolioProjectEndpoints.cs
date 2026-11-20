using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioProjectEndpoints
{
    public static void MapPortfolioProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("project", async (
            [FromServices] PortfolioProjectService projectService,
            CancellationToken cancellationToken,
            bool includeTools = false) =>
            (await projectService.GetAllProjectsAsync(includeTools, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioProjects")
        .WithTags("Portfolio")
        .Produces<List<ProjectDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
