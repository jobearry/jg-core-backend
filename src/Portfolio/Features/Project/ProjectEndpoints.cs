using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Endpoints;

namespace Portfolio.Features.Project;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(
        this IEndpointRouteBuilder routes,
        string? adminRateLimitPolicy = null,
        string? adminCorsPolicy = null)
    {
        routes.MapGet("project", async (
                GetProjectHandler handler,
                CancellationToken cancellationToken,
                bool includeTools = false) =>
            (await handler.HandleAsync(includeTools, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioProjects")
        .WithSummary("Get all projects")
        .WithDescription("Returns all projects. Optionally include tech stacks.")
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
        .WithSummary("Get a project by id")
        .WithDescription("Returns a single project including tech stacks.")
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

        if (adminCorsPolicy is not null)
        {
            admin.RequireCors(adminCorsPolicy);
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
        .WithSummary("Create a standalone project")
        .WithDescription("Creates a project with no experience link. Tech stacks are empty.")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPost("experience/{experienceId:int}/project", async (
                int experienceId,
                CreateProjectForExperienceHandler handler,
                LinkGenerator links,
                CreateProjectRequest? request,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(experienceId, request, cancellationToken))
                .ToCreatedHttpResult(dto =>
                    links.GetPathByName("GetPortfolioProjectById", new { id = dto.Id })
                        ?? throw new InvalidOperationException(
                            "The project detail route is not registered.")))
        .WithName("AdminCreateProjectForExperience")
        .WithSummary("Create a project under an experience")
        .WithDescription("Creates a project and attaches it to the specified experience. The experience must exist.")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("project/{id:int}", async (
                int id,
                UpdateProjectRequest? request,
                UpdateProjectHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateProject")
        .WithSummary("Update a project's fields")
        .WithDescription("Replaces the project's name, description and contribution. Experience link and tech stacks are not changed.")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("project/{id:int}/experience", async (
                int id,
                UpdateProjectExperienceRequest? request,
                UpdateProjectExperienceHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateProjectExperience")
        .WithSummary("Update a project's experience link")
        .WithDescription("Attaches, moves or detaches an existing project. A positive experienceId attaches or moves; null detaches. The project must exist; the experience must exist when the id is non-null.")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPost("project/{id:int}/tech-stack", async (
                int id,
                AddProjectTechStackRequest? request,
                AddProjectTechStackHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminAddProjectTechStack")
        .WithSummary("Add a tech stack link to a project")
        .WithDescription("Adds a single tech stack link. The stack must exist and not already be linked. Returns the updated project.")
        .RequireAuthorization("Admin")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapDelete("project/{id:int}/tech-stack/{techStackId:int}", async (
                int id,
                int techStackId,
                RemoveProjectTechStackHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, new RemoveProjectTechStackRequest(techStackId), cancellationToken)).ToHttpResult())
        .WithName("AdminRemoveProjectTechStack")
        .WithSummary("Remove a tech stack link from a project")
        .WithDescription("Removes a single tech stack link. The stack must currently be linked. Returns the updated project.")
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
        .WithSummary("Replace a project's tech stacks")
        .WithDescription("Replaces the entire set of tech stacks with the provided stack ids. All ids must exist. An empty array clears the stacks.")
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
        .WithSummary("Delete a project")
        .WithDescription("Deletes the project and its tech stack links.")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
