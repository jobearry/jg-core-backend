using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Core.Infrastructure.Options;
using Microsoft.IdentityModel.Tokens;

namespace Portfolio.Api.Tests;

/// <summary>
/// Policy metadata is not enough: this proves an authenticated caller with the old
/// User role is actually rejected by an admin HTTP route.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class AdminAuthorizationHttpTests : IClassFixture<JGBackendApiFactory>
{
    private readonly JGBackendApiFactory _factory;

    public AdminAuthorizationHttpTests(JGBackendApiFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task AdminDelete_WithUserRoleToken_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateUserRoleToken());

        var response = await client.DeleteAsync("/api/v1/admin/experience/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string CreateUserRoleToken()
    {
        var key = Encoding.UTF8.GetBytes(JGBackendApiFactory.SigningKey);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "portfolio-api"),
                new Claim("role", "User")
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = JGBackendApiFactory.JwtIssuer,
            Audience = JGBackendApiFactory.JwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
