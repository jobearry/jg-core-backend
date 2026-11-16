using System.Security.Claims;
using Generic.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Generic.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("login", IResult (DTOAuth? auth, IJwtTokenService tokenService) =>
            {
                var validationError = Validate(auth);
                if (validationError is not null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Validation failed",
                        detail: validationError);
                }

                var result = tokenService.CreateToken(auth!.Key);
                return result.Status switch
                {
                    AuthTokenStatus.Success => Results.Ok(new { token = result.Token }),
                    AuthTokenStatus.KeyTooShort => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Server configuration error",
                        detail: "JWT key too short."),
                    _ => Results.Unauthorized()
                };
            })
            .RequireRateLimiting(RateLimitPolicies.AuthLogin)
            .WithName("Login")
            .WithSummary("Exchange the shared key for a JWT")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("me", (ClaimsPrincipal user) =>
            {
                var allClaims = user.Claims
                    .GroupBy(c => c.Type)
                    .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToArray());

                return Results.Ok(allClaims);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Get current user info from token claims");

        return endpoints;
    }

    private static string? Validate(DTOAuth? auth)
    {
        if (auth is null || string.IsNullOrWhiteSpace(auth.Key))
        {
            return "Key is required.";
        }

        if (auth.Key.Length > 256)
        {
            return "Key exceeds the maximum allowed length of 256 characters.";
        }

        return null;
    }
}
