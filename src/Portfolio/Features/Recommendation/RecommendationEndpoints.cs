using Microsoft.AspNetCore.Authorization;
using Portfolio.Endpoints;

namespace Portfolio.Features.Recommendation;

public static class RecommendationEndpoints
{
    public static void MapRecommendationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("recommendation", async (
                GetRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioRecommendations")
        .WithTags("Portfolio")
        .AllowAnonymous()
        .Produces<List<RecommendationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
