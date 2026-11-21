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
}
