using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Portfolio.Api.Tests;

/// <summary>
/// Plan 2.5 — the auth endpoints. They are a service-token flow, not a user login:
/// there is one shared key and no user store. The portfolio endpoints stay anonymous on
/// purpose, so this is the only protected route.
/// </summary>
public class AuthEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    /// <summary>Matches <c>Jwt:Key</c> in <see cref="PortfolioApiFactory"/>.</summary>
    private const string Key = "api-tests-signing-key-that-is-long-enough";

    private readonly HttpClient _client;

    public AuthEndpointsTests(PortfolioApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithTheConfiguredKey_ReturnsAToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { key = Key });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();

        // The wire shape is unchanged: { "token": "…" }, and it is a signed JWT.
        Assert.NotNull(token);
        Assert.Equal(3, token!.Split('.').Length);
    }

    /// <summary>
    /// Minimal APIs do not run model validation, so this guard is the validation. It
    /// answers in the same ProblemDetails shape as every other failure rather than with a
    /// bare 400.
    /// </summary>
    [Fact]
    public async Task Login_WithABlankKey_Returns400WithAValidationCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { key = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_WithAWrongKey_Returns401WithACredentialsCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { key = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_credentials", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Me_WithoutAToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// The token used to carry only a role, so <c>User.Identity.Name</c> was always null.
    /// It now carries a subject, and the validation parameters are told to read it.
    /// </summary>
    [Fact]
    public async Task Me_WithATokenFromLogin_ReportsTheSubjectAndRole()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { key = Key });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var claims = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = claims.EnumerateObject().Select(property => property.Name).ToList();

        Assert.Contains("sub", names);
        Assert.Equal("portfolio-api", claims.GetProperty("sub")[0].GetString());

        // Claim type mapping is deliberately off, so accept either spelling of the role.
        var roleProperty = claims.EnumerateObject()
            .FirstOrDefault(property => property.Name.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("User", roleProperty.Value[0].GetString());
    }
}
