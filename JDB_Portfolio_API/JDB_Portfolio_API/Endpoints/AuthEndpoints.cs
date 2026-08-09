using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JDB_Portfolio_API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("api/v1/auth/login", async (IOptions<JwtOptions> opts, AuthDto auth) =>
        {
            var jwtOpts = opts.Value;
            // Implementation for login endpoint
            // Validate user credentials
            var providedKey = auth.Key ?? string.Empty;
            var configuredKey = jwtOpts.Key ?? string.Empty;

            // convert to bytes (use UTF8 if storing plain text secret), then fixed-time compare
            var providedKeyBytes = Encoding.UTF8.GetBytes(providedKey);
            var configuredKeyBytes = Encoding.UTF8.GetBytes(configuredKey);

            if (!CryptographicOperations.FixedTimeEquals(providedKeyBytes, configuredKeyBytes))
                return Results.Unauthorized();

            // validate minimum length before creating token
            if (configuredKeyBytes.Length < 32)
                return Results.Problem("Server configuration error: JWT key too short.", statusCode: 500);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = Encoding.UTF8.GetBytes(configuredKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    //TODO add role and user validator for each token generation
                    new Claim(ClaimTypes.Role, "User")
                }),
                Expires = DateTime.UtcNow.AddMinutes(jwtOpts.ExpirationInMinutes),
                Issuer = jwtOpts.Issuer,
                Audience = jwtOpts.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(jwtKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Results.Ok(new { token = tokenHandler.WriteToken(token) });
        }).WithTags("Auth");

        routes.MapGet("api/v1/auth/me", async (HttpContext context) =>
        {
            var allClaims = context.User.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToArray());

            return Results.Ok(allClaims);
        }).WithTags("Auth")
          .WithDescription("View authorized claims")
          .RequireAuthorization();
    }
}
