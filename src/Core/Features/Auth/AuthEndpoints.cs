using System.Security.Claims;
using Core.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Core.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("login", IResult (AuthDto? auth, IJwtTokenService tokenService) =>
            {
                if (auth is null || string.IsNullOrWhiteSpace(auth.Key))
                {
                    return Problem(
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        "A key is required.",
                        "validation_failed");
                }

                if (auth.Key.Length > 256)
                {
                    return Problem(
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        "Key exceeds the maximum allowed length of 256 characters.",
                        "validation_failed");
                }

                var result = tokenService.CreateToken(auth.Key);
                return result.Status switch
                {
                    AuthTokenStatus.Success => Results.Ok(new AuthTokenDto(result.Token!)),
                    AuthTokenStatus.KeyTooShort => Problem(
                        StatusCodes.Status500InternalServerError,
                        "Unexpected error",
                        "Authentication is not configured correctly.",
                        "server_misconfigured"),
                    _ => Problem(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized",
                        "The provided key is not valid.",
                        "invalid_credentials")
                };
            })
            .RequireRateLimiting(RateLimitPolicies.AuthLogin)
            .WithName("Login")
            .WithSummary("Exchange the shared key for a JWT")
            .Produces<AuthTokenDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
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
            .WithName("GetCurrentClaims")
            .WithSummary("View authorized claims")
            .Produces<Dictionary<string, string[]>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static IResult Problem(int statusCode, string title, string detail, string code) =>
        Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
