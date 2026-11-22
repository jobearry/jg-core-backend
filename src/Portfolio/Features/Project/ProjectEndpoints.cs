using Microsoft.AspNetCore.Authorization;
using Portfolio.Endpoints;

namespace Portfolio.Features.Project;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("project", async (
                GetProjectHandler handler,
                CancellationToken cancellationToken,
                bool includeTools = false) =>
            (await handler.HandleAsync(includeTools, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioProjects")
        .WithTags("Portfolio")
        .AllowAnonymous()
        .Produces<List<ProjectDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
