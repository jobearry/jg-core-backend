using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioProjectEndpoints
{
    public static void MapPortfolioProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("api/v1/project", async ([FromServices] PortfolioProjectService projectService, bool includeTools = false) =>
        {
            var result = await projectService.GetAllAsync(includeTools);
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: 502);
            }
            return Results.Ok(result);
        }).WithTags("Portfolio");
        //.RequireAuthorization();
    }
}
