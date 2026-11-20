using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Overview;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract tests for the Overview slice. The keys are the contract; labels,
/// icons and ordering belong to the client.
/// </summary>
public class OverviewEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PortfolioApiFactory _factory;
    private readonly HttpClient _client;

    public OverviewEndpointsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        Seed(factory);
    }

    [Fact]
    public async Task GetOverview_ReturnsStableKeysAndCounts()
    {
        var response = await _client.GetAsync("/api/v1/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = (await response.Content.ReadFromJsonAsync<List<OverviewItemDto>>(JsonOptions))!;

        Assert.Equal(3, items.Count);
        Assert.Equal("projects", items[0].Key);
        Assert.Equal(1, items[0].Count);
        Assert.Equal("skills", items[1].Key);
        Assert.Equal(1, items[1].Count);
        Assert.Equal("recommendations", items[2].Key);
        Assert.Equal(2, items[2].Count);
    }

    [Fact]
    public async Task GetOverview_WhenTheDatabaseFails_Returns503NotAnUnhandled500()
    {
        // The overview used to catch HttpRequestException, which the repositories never
        // throw, so a real database failure escaped entirely.
        _factory.TechStacks.Failure = new FakeDbException("could not connect");

        var response = await _client.GetAsync("/api/v1/overview");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static void Seed(PortfolioApiFactory factory)
    {
        factory.TechStacks.Failure = null;
        factory.Projects.Projects.Clear();
        factory.TechStacks.TechStacks.Clear();
        factory.Recommendations.Recommendations.Clear();

        factory.TechStacks.TechStacks.Add(
            new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend"));
        factory.Projects.Projects.Add(new Project(7, "Portfolio API"));
        factory.Recommendations.Recommendations.Add(new Recommendation(
            1, "Atsushi Deguchi", "Great to work with", new Role("Engineer"),
            new DateTime(2026, 6, 27)));
        factory.Recommendations.Recommendations.Add(new Recommendation(
            2, "Kentaro Komiya", "Grateful for the hard work", null,
            new DateTime(2026, 6, 27)));
    }
}
