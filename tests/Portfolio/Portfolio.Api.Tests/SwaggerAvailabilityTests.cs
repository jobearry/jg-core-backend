using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Api.Tests;

/// <summary>
/// Swagger is off in Production by default and can be turned on per environment with
/// <c>Swagger:Enabled</c>, so an Azure smoke test does not require a code change.
/// </summary>
public class SwaggerAvailabilityTests : IClassFixture<PortfolioApiFactory>
{
    private readonly PortfolioApiFactory _factory;

    public SwaggerAvailabilityTests(PortfolioApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_IsNotServedInProductionByDefault()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_IsServedWhenExplicitlyEnabled()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Swagger:Enabled"] = "true"
                })));

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("JDB Portfolio API", await response.Content.ReadAsStringAsync());
    }
}
