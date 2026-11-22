using Microsoft.AspNetCore.Authorization;
using Portfolio.Endpoints;
using Portfolio.Features.Overview;

namespace Portfolio.Features.Notion;

public static class NotionEndpoints
{
    public static void MapNotionEndpoints(
        this IEndpointRouteBuilder routes,
        string? rateLimitPolicy = null)
    {
        var page = routes.MapGet("notion/{pageId}", async (
                GetNotionPageHandler handler,
                string pageId,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(pageId, cancellationToken)).ToHttpResult())
            .WithName("GetNotionPage")
            .WithTags("ExternalServices - Notion")
            .AllowAnonymous()
            .Produces<List<NotionPageDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        var count = routes.MapGet("notion/{pageId}/count", async (
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
            .AllowAnonymous()
            .Produces<OverviewItemDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        if (rateLimitPolicy is not null)
        {
            page.RequireRateLimiting(rateLimitPolicy);
            count.RequireRateLimiting(rateLimitPolicy);
        }
    }
}
