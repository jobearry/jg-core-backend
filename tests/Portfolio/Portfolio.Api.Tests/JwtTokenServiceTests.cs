using System.IdentityModel.Tokens.Jwt;
using Core.Features.Auth;
using Core.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Portfolio.Api.Tests;

/// <summary>
/// Core owns token construction now. The service-token claims are part of the
/// published contract, so they are pinned here.
/// </summary>
public sealed class JwtTokenServiceTests
{
    private const string Key = "a-signing-key-that-is-definitely-long-enough";

    [Fact]
    public void CreateToken_WithTheConfiguredKey_ReturnsTheServiceToken()
    {
        var result = AService(Key).CreateToken(Key);

        Assert.Equal(AuthTokenStatus.Success, result.Status);
        Assert.NotNull(result.Token);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token!);
        Assert.Equal("portfolio-api", token.Subject);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Value == "User");
    }

    [Fact]
    public void CreateToken_WithAWrongKey_IsInvalidKey()
    {
        var result = AService(Key).CreateToken("not-the-key");

        Assert.Equal(AuthTokenStatus.InvalidKey, result.Status);
        Assert.Null(result.Token);
    }

    [Fact]
    public void CreateToken_WithABlankKey_IsInvalidKey()
    {
        var result = AService(Key).CreateToken("   ");

        Assert.Equal(AuthTokenStatus.InvalidKey, result.Status);
    }

    [Fact]
    public void CreateToken_WhenTheConfiguredKeyIsTooShort_IsKeyTooShort()
    {
        var result = AService("short").CreateToken("short");

        Assert.Equal(AuthTokenStatus.KeyTooShort, result.Status);
    }

    private static JwtTokenService AService(string key) => new(Options.Create(new JwtOptions
    {
        Key = key,
        Issuer = "test-issuer",
        Audience = "test-audience",
        Expires = 60
    }));
}
