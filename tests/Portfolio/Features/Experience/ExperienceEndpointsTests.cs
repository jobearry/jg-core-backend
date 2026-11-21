using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract tests for the Experience slice. These boot the real application
/// and go over the wire, so they cover routing, model binding, status codes and
/// serialization — not just the handler's return value.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public class ExperienceEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private static readonly DateTime StartedAt = new(2023, 5, 2);
    private static readonly DateTime FinishedAt = new(2026, 6, 24);

    private readonly JGBackendApiFactory _factory;
    private readonly HttpClient _client;

    public ExperienceEndpointsTests(JGBackendApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        Seed();
    }

    [Fact]
    public async Task GetExperience_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/experience");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The contract itself: the exact keys, in order, at the endpoint level. If a
    /// field is added, removed or renamed, this fails — which is the point.
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
    /// An undeclared role is <c>null</c> on the wire, not an exception and not
    /// an empty string.
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

        Assert.Equal(FinishedAt, ended.GetProperty("finishedAt").GetDateTime());
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

    /// <summary>
    /// A database outage is 503 with a machine-readable code, and the provider's
    /// message must not leak to the client.
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
        Assert.DoesNotContain("db.internal", problem.ToString());
    }

    /// <summary>
    /// A bug is not an outage: 503 would teach a client to retry forever.
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

    /// <summary>
    /// The token has to reach the port, or a client that navigates away keeps a
    /// database connection open until the query finishes.
    /// </summary>
    [Fact]
    public async Task GetExperience_PassesARequestCancellationTokenToTheRepository()
    {
        await _client.GetAsync("/api/v1/experience");

        var token = _factory.Experience.LastCancellationToken;

        Assert.NotNull(token);
        Assert.True(token!.Value.CanBeCanceled);
    }

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> GetProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static JsonElement FindById(JsonElement array, int id) =>
        array.EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == id);

    private void Seed()
    {
        _factory.Experience.Failure = null;
        _factory.Experience.Experiences.Clear();

        var techStackRef = new TechStackRef(2, "C#", "https://example.test/csharp.svg", "Backend");

        // The current role: ongoing, declared title, with a nested project.
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
    }
}
