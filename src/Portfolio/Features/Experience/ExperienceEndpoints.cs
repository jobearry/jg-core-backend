using Microsoft.AspNetCore.Authorization;
using Portfolio.Application.DTO;
using Portfolio.Endpoints;

namespace Portfolio.Features.Experience;

public static class ExperienceEndpoints
{
    public static void MapExperienceEndpoints(this IEndpointRouteBuilder routes)
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
    }
}
