using Portfolio.Application.DTO;
using Portfolio.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Endpoints;

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
