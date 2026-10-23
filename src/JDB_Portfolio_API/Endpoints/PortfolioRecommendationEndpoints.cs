using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JDB_Portfolio_API.Endpoints;

public static class PortfolioRecommendationEndpoints
{
    public static void MapPortfolioRecommendationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("recommendation", async (
            [FromServices] PortfolioRecommendationService recService,
            CancellationToken cancellationToken) =>
            (await recService.GetAllRecommendationsAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioRecommendations")
        .WithTags("Portfolio")
        .Produces<List<RecommendationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
