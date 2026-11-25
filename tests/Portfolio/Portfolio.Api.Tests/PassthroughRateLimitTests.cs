namespace Portfolio.Api.Tests;

/// <summary>
/// The external-service passthroughs are rate limited so a public route cannot
/// amplify into unbounded Notion/GitHub calls. A fresh factory per test keeps each
/// limiter's window independent; the shared fixture fakes the upstream readers.
/// Uses RateLimitedApiFactory which enables rate limiting (base factory disables it for admin-write tests).
/// </summary>
[Collection(RateLimitedHostCollection.RateLimitedHostCollectionName)]
public sealed class PassthroughRateLimitTests : IClassFixture<RateLimitedApiFactory>
{
    private const int PermitLimit = 30;
    private readonly RateLimitedApiFactory _factory;

    public PassthroughRateLimitTests(RateLimitedApiFactory factory) => _factory = factory;

    [Fact]
    public async Task NotionEndpoint_Returns429AfterThePermitLimit()
    {
        using var client = _factory.CreateClient();

        await RateLimitAssertions.SendUntilRejectedAsync(
            () => client.GetAsync("/api/v1/notion/page-1"),
            PermitLimit);
    }

    [Fact]
    public async Task GithubEndpoint_Returns429AfterThePermitLimit()
    {
        using var client = _factory.CreateClient();

        await RateLimitAssertions.SendUntilRejectedAsync(
            () => client.GetAsync("/api/v1/github/owner/octocat/repositories"),
            PermitLimit);
    }

    [Fact]
    public async Task RejectedPassthrough_KeepsTheCorsHeader()
    {
        using var client = _factory.CreateClient();

        var rejected = await RateLimitAssertions.SendUntilRejectedAsync(
            () => client.SendAsync(RequestWithAllowedOrigin()),
            PermitLimit);

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
}
