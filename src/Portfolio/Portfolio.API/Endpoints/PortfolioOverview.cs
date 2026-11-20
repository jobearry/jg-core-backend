using Portfolio.Application.DTO;
using Portfolio.Application.Services;

namespace Portfolio.Endpoints;
public static class PortfolioOverview
{
    public static void MapOverview(this IEndpointRouteBuilder routes){
        routes.MapGet("overview", async (
            PortfolioService portfolioService,
            CancellationToken cancellationToken) =>
            (await portfolioService.GetOverview(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioOverview")
        .WithTags("Portfolio")
        .Produces<List<OverviewItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
