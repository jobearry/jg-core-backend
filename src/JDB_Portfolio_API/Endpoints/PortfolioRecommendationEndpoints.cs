using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioRecommendationEndpoints
{
    public static void MapPortfolioRecommendationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("api/v1/recommendation", async ([FromServices] PortfolioRecommendationService recService) =>
        {
            var result = await recService.GetAllRecommendationsAsync();
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: 502);
            }
            return Results.Ok(result.Value);
        }).WithTags("Portfolio");
        //.RequireAuthorization();
    }
}
