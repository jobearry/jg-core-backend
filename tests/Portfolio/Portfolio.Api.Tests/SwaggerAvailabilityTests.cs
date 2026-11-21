using System.Net;

namespace Portfolio.Api.Tests;

/// <summary>
/// Swagger is off in Production by default and can be turned on per environment with
/// <c>Swagger:Enabled</c>, so an Azure smoke test does not require a code change.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class SwaggerAvailabilityTests
{
    [Fact]
    public async Task SwaggerJson_IsNotServedInProductionByDefault()
    {
        using var factory = new JGBackendApiFactory();

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_IsServedWhenExplicitlyEnabled()
    {
        using var factory = new SwaggerEnabledApiFactory();

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Core Backend", await response.Content.ReadAsStringAsync());
    }
}
