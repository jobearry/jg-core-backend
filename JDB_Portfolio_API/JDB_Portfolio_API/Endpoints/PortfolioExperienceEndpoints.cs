using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioExperienceEndpoints
{
    public static void MapPortfolioExperienceEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("api/v1/experience", async ([FromServices] PortfolioExperienceServices expService) =>
        {
            var result = await expService.GetAllAsync();
            return Results.Ok(result);
        });
    }
}
