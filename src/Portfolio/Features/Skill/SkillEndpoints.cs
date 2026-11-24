using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Endpoints;

namespace Portfolio.Features.Skill;

public static class SkillEndpoints
{
    public static void MapSkillEndpoints(
        this IEndpointRouteBuilder routes, string? adminRateLimitPolicy = null)
    {
        // This endpoint used to ignore IsSuccess and return 200 with a null body on
        // failure, which a client could not distinguish from "no skills exist".
        routes.MapGet("skill", async (
                GetSkillHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioSkills")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<List<TechStackDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("skill/{id:int}", async (
                int id,
                GetSkillByIdHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioSkillById")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<TechStackDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("skill-category", async (
                GetSkillCategoriesHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioSkillCategories")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<List<SkillCategoryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("skill-category/{id:int}", async (
                int id,
                GetSkillCategoryByIdHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult())
        .WithName("GetPortfolioSkillCategoryById")
        .WithTags("Portfolio - Read")
        .AllowAnonymous()
        .Produces<SkillCategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var admin = routes.MapGroup("admin").WithTags("Portfolio - Write");
        if (adminRateLimitPolicy is not null)
        {
            admin.RequireRateLimiting(adminRateLimitPolicy);
        }

        admin.MapPost("skill", async (
                CreateSkillHandler handler,
                LinkGenerator links,
                CreateSkillRequest? request,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedHttpResult(dto =>
                    links.GetPathByName("GetPortfolioSkillById", new { id = dto.Id })
                        ?? throw new InvalidOperationException(
                            "The tech stack detail route is not registered.")))
        .WithName("AdminCreateSkill")
        .RequireAuthorization("Admin")
        .Produces<TechStackDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("skill/{id:int}", async (
                int id,
                UpdateSkillRequest? request,
                UpdateSkillHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateSkill")
        .RequireAuthorization("Admin")
        .Produces<TechStackDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapDelete("skill/{id:int}", async (
                int id,
                DeleteSkillHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToNoContentHttpResult())
        .WithName("AdminDeleteSkill")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPost("skill-category", async (
                CreateSkillCategoryHandler handler,
                LinkGenerator links,
                SkillCategoryRequest? request,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedHttpResult(dto =>
                    links.GetPathByName("GetPortfolioSkillCategoryById", new { id = dto.Id })
                        ?? throw new InvalidOperationException(
                            "The category detail route is not registered.")))
        .WithName("AdminCreateSkillCategory")
        .RequireAuthorization("Admin")
        .Produces<SkillCategoryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapPut("skill-category/{id:int}", async (
                int id,
                SkillCategoryRequest? request,
                UpdateSkillCategoryHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
        .WithName("AdminUpdateSkillCategory")
        .RequireAuthorization("Admin")
        .Produces<SkillCategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        admin.MapDelete("skill-category/{id:int}", async (
                int id,
                DeleteSkillCategoryHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToNoContentHttpResult())
        .WithName("AdminDeleteSkillCategory")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
