using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;

namespace JDB_Portfolio_API.Endpoints;

public static class NotionEndpoints
{
    public static void MapNotionEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("notion/{pageId}", async (
            PortfolioService portfolioService,
            string pageId,
            CancellationToken cancellationToken) =>
            (await portfolioService.GetNotionPageAsync(pageId, cancellationToken)).ToHttpResult())
        .WithName("GetNotionPage")
        .WithTags("ExternalServices - Notion")
        .Produces<List<NotionPageDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status502BadGateway);


        routes.MapGet("notion/{pageId}/count", async (
            PortfolioService portfolioService,
            string pageId,
            CancellationToken cancellationToken) =>
        {
            var result = await portfolioService.GetNotionPageAsync(pageId, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.ToHttpResult();
            }
            return Results.Ok(result.Value!.Count);
        })
        .WithName("GetNotionPageCount")
        .WithTags("ExternalServices - Notion")
        .Produces<int>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status502BadGateway);
    }
}
