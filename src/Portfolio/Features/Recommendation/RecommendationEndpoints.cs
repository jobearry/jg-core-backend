using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Endpoints;

namespace Portfolio.Features.Recommendation;

public static class RecommendationEndpoints
{
    public static void MapRecommendationEndpoints(
        this IEndpointRouteBuilder routes,
        string? adminRateLimitPolicy = null,
        string? adminCorsPolicy = null)
    {
        routes.MapGet("recommendation", async (
                GetRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioRecommendations")
        .WithSummary("Get all recommendations")
        .WithDescription("Returns all recommendations ordered by id ascending.")
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
        .WithSummary("Get a recommendation by id")
        .WithDescription("Returns a single recommendation.")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<RecommendationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var admin = routes.MapGroup("admin").WithTags("Portfolio - Write");
        if (adminRateLimitPolicy is not null)
        {
            admin.RequireRateLimiting(adminRateLimitPolicy);
        }

        if (adminCorsPolicy is not null)
        {
            admin.RequireCors(adminCorsPolicy);
        }

        admin.MapPost("recommendation", async (
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
        .WithSummary("Create a recommendation")
        .WithDescription("Creates a new testimonial with person name, description and optional career position.")
        .RequireAuthorization("Admin")
        .Produces<RecommendationDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("recommendation/{id:int}", async (
                int id,
                UpdateRecommendationRequest? request,
                UpdateRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateRecommendation")
        .WithSummary("Update a recommendation")
        .WithDescription("Replaces the recommendation's fields.")
        .RequireAuthorization("Admin")
        .Produces<RecommendationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapDelete("recommendation/{id:int}", async (
                int id,
                DeleteRecommendationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToNoContentHttpResult())
        .WithName("AdminDeleteRecommendation")
        .WithSummary("Delete a recommendation")
        .WithDescription("Deletes the recommendation.")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
