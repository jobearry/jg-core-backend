using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;

namespace JDB_Portfolio_API.Endpoints;
public static class PortfolioOverview
{
    public static void MapOverview(this IEndpointRouteBuilder routes){
        routes.MapGet("overview", async (
            PortfolioService portfolioService,
            CancellationToken cancellationToken) =>
            (await portfolioService.GetOverview(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioOverview")
        .WithTags("Portfolio")
        .Produces<List<DashboardItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
