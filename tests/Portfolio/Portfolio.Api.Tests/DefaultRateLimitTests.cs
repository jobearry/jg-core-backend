using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Api.Tests;

/// <summary>
/// The host applies a named 120/min per-IP policy to every mapped endpoint;
/// stricter endpoint policies override it. Health probes are exempt so a monitor
/// never sees 429 and mistakes it for an outage.
/// 
/// Metadata tests use the base factory (rate limiting disabled).
/// Actual 429 behaviour tests use RateLimitedApiFactory.
/// </summary>
[Collection(RateLimitedHostCollection.RateLimitedHostCollectionName)]
public sealed class DefaultRateLimitTests : IClassFixture<RateLimitedApiFactory>
{
    private const int DefaultPermitLimit = 120;
    private readonly RateLimitedApiFactory _factory;

    public DefaultRateLimitTests(RateLimitedApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PortfolioReadEndpoint_Returns429AfterTheDefaultPermitLimit()
    {
        using var client = _factory.CreateClient();

        await RateLimitAssertions.SendUntilRejectedAsync(
            () => client.GetAsync("/api/v1/skill"),
            DefaultPermitLimit);
    }

    [Fact]
    public void EveryMappedEndpoint_HasRateLimitMetadataOrIsAHealthProbe()
    {
        // Use base factory for metadata tests (rate limiting disabled)
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
        // Use base factory for metadata tests
        using var factory = new JGBackendApiFactory();
        using var scope = factory.Services.CreateScope();
        var adminEndpoints = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.Contains("admin/") == true)
            .ToList();

        Assert.Equal(20, adminEndpoints.Count);
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
        // Use base factory since health endpoints are exempt anyway
        using var factory = new JGBackendApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i <= DefaultPermitLimit; i++)
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
