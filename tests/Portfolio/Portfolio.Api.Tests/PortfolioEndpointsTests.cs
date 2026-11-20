using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.ErrorHandling;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Api.Tests;

/// <summary>
/// Endpoint contract tests (plan 2.1 / 3.5). These boot the real application and go
/// over HTTP, so they cover routing, model binding, status codes and serialization —
/// everything the mapper unit tests cannot.
///
/// <para>
/// Repositories are faked (<see cref="PortfolioApiFactory"/>), so no database is
/// involved. The seeded data is deliberately shaped to include the awkward cases: an
/// undeclared role, an undeclared career position, and a role with an end date.
/// </para>
/// </summary>
public class PortfolioEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private static readonly DateTime StartedAt = new(2023, 5, 2);
    private static readonly DateTime FinishedAt = new(2026, 6, 24);

    private readonly PortfolioApiFactory _factory;
    private readonly HttpClient _client;

    public PortfolioEndpointsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        Seed();
    }

    // ---------------------------------------------------------------- failure paths

    // ---------------------------------------------------------------------- notion

    [Fact]
    public async Task GetNotionPage_ReturnsThePageShape()
    {
        var json = await GetJsonAsync("/api/v1/notion/some-page-id");

        var page = Assert.Single(json.EnumerateArray());
        Assert.Equal("https://notion.so/page", page.GetProperty("url").GetString());
        Assert.Equal("2026-06-27", page.GetProperty("date").GetString());
        Assert.Equal("About me", page.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetNotionPage_WhenNotionIsUnreachable_Returns502WithItsOwnCode()
    {
        _factory.Notion.Failure = new HttpRequestException("no route to host");

        var response = await _client.GetAsync("/api/v1/notion/some-page-id");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var problem = await GetProblemAsync(response);
        Assert.Equal("notion_unavailable", problem.GetProperty("code").GetString());
    }

    /// <summary>
    /// Same status, different code: "Notion is down" and "Notion changed its format"
    /// need different reactions from a client.
    /// </summary>
    [Fact]
    public async Task GetNotionPage_WhenTheResponseIsUnreadable_Returns502WithADifferentCode()
    {
        _factory.Notion.Failure = new InvalidOperationException("unexpected markdown shape");

        var response = await _client.GetAsync("/api/v1/notion/some-page-id");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var problem = await GetProblemAsync(response);
        Assert.Equal("notion_invalid_response", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetNotionPageCount_ReturnsOverviewItemShape()
    {
        var json = await GetJsonAsync("/api/v1/notion/some-page-id/count");

        Assert.Equal("notionPages", json.GetProperty("key").GetString());
        Assert.Equal(1, json.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task GetNotionPageCount_WhenNotionIsUnreachable_Returns502NotOkWithZero()
    {
        _factory.Notion.Failure = new HttpRequestException("no route to host");

        var response = await _client.GetAsync("/api/v1/notion/some-page-id/count");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    /// <summary>
    /// Easy to forget and invisible when missing: without this registration
    /// <c>UseExceptionHandler()</c> has no handler to delegate to, and every unhandled
    /// exception silently becomes a bare 500 again.
    /// </summary>
    [Fact]
    public void TheGlobalExceptionHandlerIsRegistered()
    {
        Assert.Contains(
            _factory.Services.GetServices<IExceptionHandler>(),
            handler => handler is GlobalExceptionHandler);
    }

    // --------------------------------------------------------------------- helpers

    private static async Task<JsonElement> GetJsonAsync(string url, HttpClient? client = null)
    {
        var httpClient = client ?? throw new InvalidOperationException("client required");
        var response = await httpClient.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> GetJsonAsync(string url) => await GetJsonAsync(url, _client);

    private static async Task<JsonElement> GetProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static JsonElement FindById(JsonElement array, int id) =>
        array.EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == id);

    private void Seed()
    {
        _factory.Experience.Failure = null;
        _factory.TechStacks.Failure = null;
        _factory.Notion.Failure = null;
        _factory.Experience.Experiences.Clear();
        _factory.Projects.Projects.Clear();
        _factory.TechStacks.TechStacks.Clear();
        _factory.Recommendations.Recommendations.Clear();
        _factory.Notion.Pages.Clear();

        _factory.Notion.Pages.Add(new Portfolio.Application.Models.PageContent(
            "https://notion.so/page",
            "2026-06-27",
            "About me"));

        var techStack = new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend");
        _factory.TechStacks.TechStacks.Add(techStack);

        // Plan 3.4: the entity above is the shared reference row; the project cites it by
        // reference, which is what the aggregate actually holds.
        var techStackRef = new TechStackRef(2, "C#", "https://example.test/csharp.svg", "Backend");

        _factory.Projects.Projects.Add(new Project(
            7,
            null,
            "Portfolio API",
            "The API behind the site",
            "Everything",
            new[] { techStackRef }));

        // The current role: ongoing, declared title, with projects.
        var currentRole = new Experience(
            4,
            new Company("Applied ICT Inc."),
            new Period(new DateTime(2026, 6, 25), null),
            new Role("Software Engineer"));
        currentRole.ReplaceProjects(new[]
        {
            new Project(7, null, "Portfolio API", null, null, new[] { techStackRef })
        });
        _factory.Experience.Experiences.Add(currentRole);

        // The past role: ended, and the title is deliberately undeclared.
        _factory.Experience.Experiences.Add(new Experience(
            3,
            new Company("SHI Designing and Manufacturing Inc."),
            new Period(StartedAt, FinishedAt),
            null));

        _factory.Recommendations.Recommendations.Add(new Recommendation(
            1, "Atsushi Deguchi", "Great to work with", new Role("Engineer"),
            new DateTime(2026, 6, 27)));

        _factory.Recommendations.Recommendations.Add(new Recommendation(
            2, "Kentaro Komiya", "Grateful for the hard work", null,
            new DateTime(2026, 6, 27)));
    }
}
