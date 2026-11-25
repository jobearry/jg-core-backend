using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Application.DTO;
using Portfolio.Endpoints;

namespace Portfolio.Features.Experience;

public static class ExperienceEndpoints
{
    public static void MapExperienceEndpoints(
        this IEndpointRouteBuilder routes,
        string? adminRateLimitPolicy = null,
        string? adminCorsPolicy = null)
    {
        // The token is bound to RequestAborted. Passing it all the way down means a
        // client that navigates away stops the query instead of holding a connection.
        routes.MapGet("experience", async (
                GetExperienceHandler handler,
                CancellationToken cancellationToken,
                bool includeProjects = false) =>
            (await handler.HandleAsync(includeProjects, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioExperience")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<List<ExperienceDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("experience/{id:int}", async (
                int id,
                GetExperienceByIdHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioExperienceById")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<ExperienceDto>(StatusCodes.Status200OK)
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

        admin.MapPost("experience", async (
                CreateExperienceHandler handler,
                LinkGenerator links,
                CreateExperienceRequest? request,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedHttpResult(dto =>
                    links.GetPathByName("GetPortfolioExperienceById", new { id = dto.Id })
                        ?? throw new InvalidOperationException(
                            "The experience detail route is not registered.")))
        .WithName("AdminCreateExperience")
        .RequireAuthorization("Admin")
        .Produces<ExperienceDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("experience/{id:int}", async (
                int id,
                UpdateExperienceRequest? request,
                UpdateExperienceHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateExperience")
        .RequireAuthorization("Admin")
        .Produces<ExperienceDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapDelete("experience/{id:int}", async (
                int id,
                DeleteExperienceHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToNoContentHttpResult())
        .WithName("AdminDeleteExperience")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
