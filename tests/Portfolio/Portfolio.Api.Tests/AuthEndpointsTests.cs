using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Portfolio.Api.Tests;

/// <summary>
/// Auth is now owned by Core and mapped by JGBackend. These tests lock the
/// published contract so the host swap is invisible to clients.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class AuthEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(JGBackendApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithTheConfiguredKey_ReturnsAToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new { key = JGBackendApiFactory.SigningKey });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();

        Assert.NotNull(token);
        Assert.Equal(3, token!.Split('.').Length);
    }

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

    [Fact]
    public async Task Me_WithATokenFromLogin_ReportsTheSubjectAndRole()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new { key = JGBackendApiFactory.SigningKey });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var claims = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = claims.EnumerateObject().Select(property => property.Name).ToList();

        Assert.Contains("sub", names);
        Assert.Equal("portfolio-api", claims.GetProperty("sub")[0].GetString());

        var roleProperty = claims.EnumerateObject()
            .FirstOrDefault(property => property.Name.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("User", roleProperty.Value[0].GetString());
    }
}
