using Portfolio.Application.DTO;
using Portfolio.Application.Services;

namespace Portfolio.Endpoints;

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
            return Results.Ok(new OverviewItemDto("notionPages", result.Value!.Count));
        })
        .WithName("GetNotionPageCount")
        .WithTags("ExternalServices - Notion")
        .Produces<OverviewItemDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status502BadGateway);
    }
}
