using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Api.Tests;

/// <summary>
/// The host applies a named 120/min per-IP policy to every mapped endpoint;
/// stricter endpoint policies override it. Health probes are exempt so a monitor
/// never sees 429 and mistakes it for an outage.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class DefaultRateLimitTests
{
    private const int DefaultPermitLimit = 120;

    [Fact]
    public async Task PortfolioReadEndpoint_Returns429AfterTheDefaultPermitLimit()
    {
        using var factory = new JGBackendApiFactory();
        var client = factory.CreateClient();

        await RateLimitAssertions.SendUntilRejectedAsync(
            () => client.GetAsync("/api/v1/skill"),
            DefaultPermitLimit);
    }

    [Fact]
    public void EveryMappedEndpoint_HasRateLimitMetadataOrIsAHealthProbe()
    {
        using var factory = new JGBackendApiFactory();
        using var scope = factory.Services.CreateScope();
        var endpoints = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>();

        foreach (var endpoint in endpoints)
        {
            var route = endpoint.RoutePattern.RawText?.TrimStart('/');
            if (route is "healthz" or "api/v1/health")
            {
                Assert.NotNull(endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>());
                continue;
            }

            Assert.NotNull(endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>());
        }
    }

    [Fact]
    public void AdminWriteEndpoints_UseTheAdminWritePolicy()
    {
        using var factory = new JGBackendApiFactory();
        using var scope = factory.Services.CreateScope();
        var adminEndpoints = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.Contains("admin/") == true)
            .ToList();

        Assert.Equal(18, adminEndpoints.Count);
        Assert.All(
            adminEndpoints,
            endpoint => Assert.Equal(
                "admin-write",
                endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName));
    }

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/api/v1/health")]
    public async Task HealthEndpoints_AreNotRateLimited(string path)
    {
        using var factory = new JGBackendApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i <= DefaultPermitLimit; i++)
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
