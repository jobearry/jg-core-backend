using JDB_Portfolio_API.Application.Services;

namespace JDB_Portfolio_API.Endpoints;

public static class NotionEndpoints
{
    public static void MapNotionEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("api/v1/notion/{pageId}", async (PortfolioService portfolioService, string pageId) =>
        {
            var result = await portfolioService.GetNotionPageAsync(pageId);
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: 502);
            }
            return Results.Ok(result.Value);
        })
        .WithTags("ExternalServices - Notion");
        // .RequireAuthorization();
            

        routes.MapGet("api/v1/notion/{pageId}/count", async (PortfolioService portfolioService, string pageId) =>
        {
            var result = await portfolioService.GetNotionPageAsync(pageId);
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: 502);
            }
            return Results.Ok(result.Value!.Count);
        })
        .WithTags("ExternalServices - Notion");
        // .RequireAuthorization();
    }
}
