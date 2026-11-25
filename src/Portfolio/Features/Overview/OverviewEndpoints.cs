using Microsoft.AspNetCore.Authorization;
using Portfolio.Endpoints;

namespace Portfolio.Features.Overview;

public static class OverviewEndpoints
{
    public static void MapOverviewEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("overview", async (
                GetOverviewHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioOverview")
        .WithSummary("Get the portfolio overview")
        .WithDescription("Returns a summary of all portfolio sections with counts.")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<List<OverviewItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
