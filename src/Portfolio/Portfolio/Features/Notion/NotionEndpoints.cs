using Portfolio.Endpoints;
using Portfolio.Features.Overview;

namespace Portfolio.Features.Notion;

public static class NotionEndpoints
{
    public static void MapNotionEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("notion/{pageId}", async (
                GetNotionPageHandler handler,
                string pageId,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(pageId, cancellationToken)).ToHttpResult())
        .WithName("GetNotionPage")
        .WithTags("ExternalServices - Notion")
        .Produces<List<NotionPageDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status502BadGateway);

        routes.MapGet("notion/{pageId}/count", async (
                GetNotionPageHandler handler,
                string pageId,
                CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(pageId, cancellationToken);
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
