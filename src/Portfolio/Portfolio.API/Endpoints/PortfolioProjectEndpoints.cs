using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Endpoints;

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
