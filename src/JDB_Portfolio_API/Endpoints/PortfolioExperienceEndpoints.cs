using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioExperienceEndpoints
{
    public static void MapPortfolioExperienceEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("api/v1/experience", async ([FromServices] PortfolioExperienceService expService, bool includeProjects = false) =>
        {
            var result = await expService.GetAllExperienceAsync(includeProjects);
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: 502);
            }
            return Results.Ok(result.Value);
        }).WithTags("Portfolio");
          //.RequireAuthorization();
    }
}
