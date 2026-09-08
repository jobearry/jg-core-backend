using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioSkillEndpoints
{
    public static void MapPortfolioSkillEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/skill", async ([FromServices] PortfolioSkillService skillService) =>
        {
            var skills = await skillService.GetAllTechStacksAsync();
            return Results.Ok(skills.Value);
        })
        .WithTags("Portfolio");
    }
}
