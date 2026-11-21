using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Features.Github;
using Portfolio.Features.Notion;

namespace Portfolio.Api.Tests;

/// <summary>
/// The external-service passthroughs are rate limited so a public route cannot
/// amplify into unbounded Notion/GitHub calls. Fakes keep each request local and
/// make the only variable the limiter.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class PassthroughRateLimitTests
{
    private const int PermitLimit = 30;

    [Fact]
    public async Task NotionEndpoint_Returns429AfterThePermitLimit()
    {
        using var factory = new RateLimitFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < PermitLimit; i++)
        {
            var allowed = await client.GetAsync("/api/v1/notion/page-1");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var rejected = await client.GetAsync("/api/v1/notion/page-1");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task GithubEndpoint_Returns429AfterThePermitLimit()
    {
        using var factory = new RateLimitFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < PermitLimit; i++)
        {
            var allowed = await client.GetAsync("/api/v1/github/owner/octocat/repositories");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var rejected = await client.GetAsync("/api/v1/github/owner/octocat/repositories");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task RejectedPassthrough_KeepsTheCorsHeader()
    {
        using var factory = new RateLimitFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < PermitLimit; i++)
        {
            await client.SendAsync(RequestWithAllowedOrigin());
        }

        var rejected = await client.SendAsync(RequestWithAllowedOrigin());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Contains(
            "https://jgolimlim-portfolio.vercel.app",
            rejected.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage RequestWithAllowedOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notion/page-1");
        request.Headers.Add("Origin", "https://jgolimlim-portfolio.vercel.app");
        return request;
    }

    private sealed class RateLimitFactory : JGBackendApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPageContentReader>();
                services.RemoveAll<IGithubRepositoryReader>();
                services.AddSingleton<IPageContentReader>(new FakePageContentReader());
                services.AddSingleton<IGithubRepositoryReader>(new FakeGithubRepositoryReader());
            });
        }
    }
}
