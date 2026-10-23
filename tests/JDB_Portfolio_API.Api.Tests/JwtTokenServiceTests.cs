using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.Settings;
using JDB_Portfolio_API.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace JDB_Portfolio_API.Api.Tests;

/// <summary>
/// Plan 2.5 — token construction moved out of the endpoint, so it can be tested without
/// booting HTTP.
/// </summary>
public class JwtTokenServiceTests
{
    private const string Key = "a-signing-key-that-is-definitely-long-enough";

    [Fact]
    public void IssueToken_WithTheConfiguredKey_ReturnsATokenForTheServiceSubject()
    {
        var service = AService(Key);

        var result = service.IssueToken(Key);

        Assert.True(result.IsSuccess);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);
        Assert.Equal("portfolio-api", token.Subject);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Value == "User");
    }

    /// <summary>
    /// A wrong key is a 401, not a 400 and not a 500. The message says nothing about
    /// which part was wrong, and the code is stable so a client can act on it.
    /// </summary>
    [Fact]
    public void IssueToken_WithAWrongKey_IsUnauthorized()
    {
        var service = AService(Key);

        var result = service.IssueToken("not-the-key");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Equal("invalid_credentials", result.Error.Code);
    }

    [Fact]
    public void IssueToken_WithABlankKey_IsValidationNotUnauthorized()
    {
        var service = AService(Key);

        var result = service.IssueToken("   ");

        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    /// <summary>
    /// A secret too short for HS256 is a server misconfiguration — a 500. It is checked
    /// only after the key matches, so an anonymous caller cannot use the response to
    /// learn the length of the real secret.
    /// </summary>
    [Fact]
    public void IssueToken_WhenTheConfiguredKeyIsTooShort_IsUnexpectedWithItsOwnCode()
    {
        var service = AService("short");

        var result = service.IssueToken("short");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unexpected, result.Error!.Kind);
        Assert.Equal("server_misconfigured", result.Error.Code);
    }

    private static JwtTokenService AService(string key) => new(Options.Create(new JwtOptions
    {
        Key = key,
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpirationInMinutes = 60
    }));
}
