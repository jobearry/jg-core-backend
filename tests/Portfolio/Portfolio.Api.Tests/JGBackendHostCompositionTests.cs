extern alias JGBackend;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Portfolio.Api.Tests;

public sealed class JGBackendHostCompositionTests
{
    [Fact]
    public void MapPortfolioFeatures_MapsPortfolioRouteUnderApiV1()
    {
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings__JDBConnection"] =
                "Host=unused;Database=unused;Username=unused;Password=unused",
            ["Jwt__Key"] = "host-composition-tests-signing-key-long-enough",
            ["Jwt__Issuer"] = "host-composition-tests",
            ["Jwt__Audience"] = "host-composition-tests",
            ["Jwt__Expires"] = "60"
        };
        var originals = settings.ToDictionary(
            setting => setting.Key,
            setting => Environment.GetEnvironmentVariable(setting.Key));

        try
        {
            foreach (var setting in settings)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }

            using var factory = new WebApplicationFactory<JGBackend::Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Production));

            using var scope = factory.Services.CreateScope();
            var endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints;

            Assert.Contains(endpoints, endpoint =>
                endpoint is RouteEndpoint route &&
                route.RoutePattern.RawText == "api/v1/experience");
        }
        finally
        {
            foreach (var original in originals)
            {
                Environment.SetEnvironmentVariable(original.Key, original.Value);
            }
        }
    }
}
