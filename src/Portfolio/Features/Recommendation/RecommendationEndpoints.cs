using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Endpoints;

namespace Portfolio.Features.Recommendation;

public static class RecommendationEndpoints
{
    public static void MapRecommendationEndpoints(
        this IEndpointRouteBuilder routes,
        string? adminRateLimitPolicy = null)
    {
        routes.MapGet("recommendation", async (
                GetRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioRecommendations")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<List<RecommendationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("recommendation/{id:int}", async (
                int id,
                GetRecommendationByIdHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioRecommendationById")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<RecommendationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var admin = routes.MapGroup("admin").WithTags("Portfolio - Write");

        var create = admin.MapPost("recommendation", async (
                CreateRecommendationHandler handler,
                LinkGenerator links,
                CreateRecommendationRequest? request,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedHttpResult(dto =>
                    links.GetPathByName("GetPortfolioRecommendationById", new { id = dto.Id })
                        ?? throw new InvalidOperationException(
                            "The recommendation detail route is not registered.")))
        .WithName("AdminCreateRecommendation")
        .RequireAuthorization("Admin")
        .Produces<RecommendationDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var update = admin.MapPut("recommendation/{id:int}", async (
                int id,
                UpdateRecommendationRequest? request,
                UpdateRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateRecommendation")
        .RequireAuthorization("Admin")
        .Produces<RecommendationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var delete = admin.MapDelete("recommendation/{id:int}", async (
                int id,
                DeleteRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToNoContentHttpResult())
        .WithName("AdminDeleteRecommendation")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        if (adminRateLimitPolicy is not null)
        {
            create.RequireRateLimiting(adminRateLimitPolicy);
            update.RequireRateLimiting(adminRateLimitPolicy);
            delete.RequireRateLimiting(adminRateLimitPolicy);
        }
    }
}
