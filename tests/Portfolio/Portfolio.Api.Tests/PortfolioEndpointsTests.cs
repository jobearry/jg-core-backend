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

    // ------------------------------------------------------------------ experience

    [Fact]
    public async Task GetExperience_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/experience");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The contract itself: the exact keys, in order, at the endpoint level. If a field
    /// is added, removed or renamed, this fails — which is the point.
    /// </summary>
    [Fact]
    public async Task GetExperience_ReturnsTheFlattenedDtoContract()
    {
        var json = await GetJsonAsync("/api/v1/experience");
        var first = json[0];

        var keys = first.EnumerateObject().Select(property => property.Name).ToList();

        Assert.Equal(
            new[]
            {
                "id", "company", "role", "startedAt", "finishedAt", "durationMonths",
                "description", "responsibility", "type", "projects"
            },
            keys);

        // The old shape: a nested duration object carrying a TimeSpan string.
        Assert.False(first.TryGetProperty("duration", out _));
    }

    [Fact]
    public async Task GetExperience_FlattensCompanyAndRoleToStrings()
    {
        var json = await GetJsonAsync("/api/v1/experience");
        var first = json[0];

        Assert.Equal(JsonValueKind.String, first.GetProperty("company").ValueKind);
        Assert.Equal("Applied ICT Inc.", first.GetProperty("company").GetString());

        Assert.Equal(JsonValueKind.String, first.GetProperty("role").ValueKind);
        Assert.Equal("Software Engineer", first.GetProperty("role").GetString());
    }

    /// <summary>
    /// The 1.5 behaviour, seen from the wire: an undeclared role is <c>null</c>, not an
    /// exception and not an empty string.
    /// </summary>
    [Fact]
    public async Task GetExperience_WithUndeclaredRole_ReturnsNullRole()
    {
        var json = await GetJsonAsync("/api/v1/experience");
        var undeclared = FindById(json, 3);

        Assert.Equal(JsonValueKind.Null, undeclared.GetProperty("role").ValueKind);
    }

    [Fact]
    public async Task GetExperience_WithFinishedAt_ReportsCalendarMonths()
    {
        var json = await GetJsonAsync("/api/v1/experience");
        var ended = FindById(json, 3);

        // 2023-05-02 → 2026-06-24 is 37 whole calendar months.
        Assert.Equal(37, ended.GetProperty("durationMonths").GetInt32());

        Assert.Equal(
            FinishedAt,
            ended.GetProperty("finishedAt").GetDateTime());
    }

    [Fact]
    public async Task GetExperience_WithIncludeProjects_ReturnsNestedProjectsWithoutInternalFields()
    {
        var json = await GetJsonAsync("/api/v1/experience?includeProjects=true");
        var withProjects = FindById(json, 4);

        var project = Assert.Single(withProjects.GetProperty("projects").EnumerateArray());

        Assert.Equal("Portfolio API", project.GetProperty("projectName").GetString());
        Assert.False(project.TryGetProperty("experienceId", out _));
        Assert.False(project.TryGetProperty("period", out _));

        var techStack = Assert.Single(project.GetProperty("techStacks").EnumerateArray());
        Assert.Equal("C#", techStack.GetProperty("stackName").GetString());
        Assert.False(techStack.TryGetProperty("createdAt", out _));
    }

    // -------------------------------------------------------------- other endpoints

    [Fact]
    public async Task GetProject_DoesNotExposeExperienceIdOrPeriod()
    {
        var json = await GetJsonAsync("/api/v1/project");
        var project = Assert.Single(json.EnumerateArray());

        Assert.False(project.TryGetProperty("experienceId", out _));
        Assert.False(project.TryGetProperty("period", out _));
    }

    [Fact]
    public async Task GetSkill_DoesNotExposeCreatedAt()
    {
        var json = await GetJsonAsync("/api/v1/skill");
        var techStack = Assert.Single(json.EnumerateArray());

        Assert.Equal("C#", techStack.GetProperty("stackName").GetString());
        Assert.False(techStack.TryGetProperty("createdAt", out _));
    }

    /// <summary>
    /// Plan 2.5. This endpoint used to ignore <c>IsSuccess</c> and answer <c>200</c> with
    /// a null body on failure, which a client could not tell apart from "there are no
    /// skills". It goes through the shared mapper now.
    /// </summary>
    [Fact]
    public async Task GetSkill_WhenTheDatabaseFails_Returns503NotAnEmpty200()
    {
        _factory.TechStacks.Failure = new FakeDbException("could not connect");

        var response = await _client.GetAsync("/api/v1/skill");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var problem = await GetProblemAsync(response);
        Assert.Equal("database_unavailable", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetRecommendation_FlattensCareerPositionAndAllowsUndeclared()
    {
        var json = await GetJsonAsync("/api/v1/recommendation");

        var declared = FindById(json, 1);
        Assert.Equal("Engineer", declared.GetProperty("careerPosition").GetString());

        var undeclared = FindById(json, 2);
        Assert.Equal(JsonValueKind.Null, undeclared.GetProperty("careerPosition").ValueKind);
    }

    [Fact]
    public async Task GetOverview_ReturnsStableKeysAndCounts()
    {
        var json = await GetJsonAsync("/api/v1/overview");

        Assert.Equal(3, json.GetArrayLength());

        // Plan 4.x: the key is the contract; labels/icons/order live in the client.
        Assert.Equal("projects", json[0].GetProperty("key").GetString());
        Assert.Equal(1, json[0].GetProperty("count").GetInt32());
        Assert.Equal("skills", json[1].GetProperty("key").GetString());
        Assert.Equal(1, json[1].GetProperty("count").GetInt32());
        Assert.Equal("recommendations", json[2].GetProperty("key").GetString());
        Assert.Equal(2, json[2].GetProperty("count").GetInt32());
    }

    // ---------------------------------------------------------------- failure paths

    /// <summary>
    /// Plan 2.2. A database outage used to come back as <c>502 Bad Gateway</c> — wrong
    /// twice over: it is not a gateway, and the client could not tell it apart from a
    /// Notion failure. It is <c>503</c> now, with a machine-readable code.
    /// </summary>
    [Fact]
    public async Task GetExperience_WhenTheDatabaseFails_Returns503WithADatabaseCode()
    {
        _factory.Experience.Failure = new FakeDbException(
            "could not connect to host db.internal as user jg");

        var response = await _client.GetAsync("/api/v1/experience");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var problem = await GetProblemAsync(response);
        Assert.Equal(503, problem.GetProperty("status").GetInt32());
        Assert.Equal("database_unavailable", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));

        // The provider's message must not reach the client.
        Assert.DoesNotContain("db.internal", problem.ToString());
    }

    /// <summary>
    /// A bug is not an outage. If this returned 503 a client would retry forever.
    /// </summary>
    [Fact]
    public async Task GetExperience_WhenABugThrows_Returns500WithAnUnexpectedCode()
    {
        _factory.Experience.Failure = new NullReferenceException("Object reference not set");

        var response = await _client.GetAsync("/api/v1/experience");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = await GetProblemAsync(response);
        Assert.Equal("unexpected_error", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetOverview_WhenTheDatabaseFails_Returns503NotAnUnhandled500()
    {
        // GetOverview used to catch HttpRequestException, which the repositories never
        // throw, so a real database failure escaped entirely.
        _factory.TechStacks.Failure = new FakeDbException("could not connect");

        var response = await _client.GetAsync("/api/v1/overview");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

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

    // ------------------------------------------------------- plan 2.4 (pipeline)

    /// <summary>
    /// The token has to reach the port, or a client that navigates away keeps a database
    /// connection open until the query finishes.
    /// </summary>
    [Fact]
    public async Task GetExperience_PassesARequestCancellationTokenToTheRepository()
    {
        await _client.GetAsync("/api/v1/experience");

        var token = _factory.Experience.LastCancellationToken;

        Assert.NotNull(token);
        Assert.True(token!.Value.CanBeCanceled);
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
