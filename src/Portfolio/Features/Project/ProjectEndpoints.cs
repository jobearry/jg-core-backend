using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Endpoints;

namespace Portfolio.Features.Project;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(
        this IEndpointRouteBuilder routes, string? adminRateLimitPolicy = null)
    {
        routes.MapGet("project", async (
                GetProjectHandler handler,
                CancellationToken cancellationToken,
                bool includeTools = false) =>
            (await handler.HandleAsync(includeTools, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioProjects")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<List<ProjectDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("project/{id:int}", async (
                int id,
                GetProjectByIdHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioProjectById")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var admin = routes.MapGroup("admin").WithTags("Portfolio - Write");
        if (adminRateLimitPolicy is not null)
        {
            admin.RequireRateLimiting(adminRateLimitPolicy);
        }

        admin.MapPost("project", async (
                CreateProjectHandler handler,
                LinkGenerator links,
                CreateProjectRequest? request,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedHttpResult(dto =>
                    links.GetPathByName("GetPortfolioProjectById", new { id = dto.Id })
                        ?? throw new InvalidOperationException(
                            "The project detail route is not registered.")))
        .WithName("AdminCreateProject")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("project/{id:int}", async (
                int id,
                UpdateProjectRequest? request,
                UpdateProjectHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateProject")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("project/{id:int}/tech-stacks", async (
                int id,
                ReplaceProjectTechStacksRequest? request,
                ReplaceProjectTechStacksHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminReplaceProjectTechStacks")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapDelete("project/{id:int}", async (
                int id,
                DeleteProjectHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToNoContentHttpResult())
        .WithName("AdminDeleteProject")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
