using System.Text.Json;

namespace Portfolio.Api.Tests;

/// <summary>
/// The host swap must not change the published operation names or schema names.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class JGBackendSwaggerContractTests : IClassFixture<SwaggerEnabledApiFactory>
{
    private readonly HttpClient _client;

    public JGBackendSwaggerContractTests(SwaggerEnabledApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_KeepsTheAuthOperationNamesAndSchemaNames()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);

        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal(
            "Login",
            paths.GetProperty("/api/v1/auth/login").GetProperty("post").GetProperty("operationId").GetString());
        Assert.Equal(
            "GetCurrentClaims",
            paths.GetProperty("/api/v1/auth/me").GetProperty("get").GetProperty("operationId").GetString());

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("AuthDto", out _));
        Assert.True(schemas.TryGetProperty("AuthTokenDto", out _));
    }

    [Fact]
    public async Task Swagger_GroupsPortfolioEndpointsIntoReadAndWrite()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        foreach (var path in new[]
        {
            "/api/v1/experience",
            "/api/v1/experience/{id}",
            "/api/v1/project",
            "/api/v1/project/{id}",
            "/api/v1/recommendation",
            "/api/v1/recommendation/{id}",
            "/api/v1/skill",
            "/api/v1/skill-category",
            "/api/v1/overview"
        })
        {
            Assert.Equal("Portfolio - Read", FirstTag(paths, path, "get"));
        }

        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/recommendation", "post"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/recommendation/{id}", "put"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/recommendation/{id}", "delete"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/experience", "post"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/experience/{id}", "put"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/experience/{id}", "delete"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/project", "post"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/project/{id}", "put"));
        Assert.Equal("Portfolio - Write", FirstTag(paths, "/api/v1/admin/project/{id}", "delete"));
    }

    private static string? FirstTag(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("tags")[0].GetString();
}
