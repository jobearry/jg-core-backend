using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Api.Tests;

[Collection(JGBackendHostCollection.Name)]
public sealed class JGBackendHostCompositionTests
{
    [Fact]
    public void MapPortfolioFeatures_MapsPortfolioRouteUnderApiV1()
    {
        using var factory = new JGBackendApiFactory();

        using var scope = factory.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints;

        Assert.Contains(endpoints, endpoint =>
            endpoint is RouteEndpoint route &&
            route.RoutePattern.RawText == "api/v1/experience");
    }
}
