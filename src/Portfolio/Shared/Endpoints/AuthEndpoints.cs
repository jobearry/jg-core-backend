using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Services;

namespace Portfolio.Endpoints;

/// <summary>
/// The service-token endpoints.
/// </summary>
/// <remarks>
/// Portfolio, Notion, and current GitHub read endpoints are intentionally anonymous.
/// This service-token flow is available for the protected /auth/me endpoint and future
/// authenticated features; it is not a user login because there is no user store and
/// only one shared key.
/// </remarks>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("auth/login", (IAuthTokenService tokenService, AuthDto auth) =>
        {
            // Minimal APIs do not run model validation, so this is the guard. It returns
            // the same ProblemDetails shape as every other failure instead of a bare 400.
            if (string.IsNullOrWhiteSpace(auth.Key))
            {
                return Error.Validation("A key is required.").ToHttpResult();
            }

            var result = tokenService.IssueToken(auth.Key);

            return result.IsSuccess
                ? Results.Ok(new AuthTokenDto(result.Value!))
                : result.ToHttpResult();
        })
        .WithName("Login")
        .WithTags("Auth")
        .Produces<AuthTokenDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        routes.MapGet("auth/me", (HttpContext context) =>
        {
            var allClaims = context.User.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToArray());

            return Results.Ok(allClaims);
        })
        .WithName("GetCurrentClaims")
        .WithTags("Auth")
        .WithDescription("View authorized claims")
        .Produces<Dictionary<string, string[]>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
