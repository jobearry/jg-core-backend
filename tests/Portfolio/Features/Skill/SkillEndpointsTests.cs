using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Domain.Entities;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract tests for the Skill slice. The published stack shape must not
/// include the database's audit column.
/// </summary>
public class SkillEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly PortfolioApiFactory _factory;
    private readonly HttpClient _client;

    public SkillEndpointsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        Seed(factory);
    }

    [Fact]
    public async Task GetSkill_DoesNotExposeCreatedAt()
    {
        var response = await _client.GetAsync("/api/v1/skill");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var techStack = Assert.Single(json.EnumerateArray());

        Assert.Equal("C#", techStack.GetProperty("stackName").GetString());
        Assert.False(techStack.TryGetProperty("createdAt", out _));
    }

    /// <summary>
    /// This endpoint used to ignore <c>IsSuccess</c> and answer 200 with a null body
    /// on failure, which a client could not tell apart from "there are no skills".
    /// </summary>
    [Fact]
    public async Task GetSkill_WhenTheDatabaseFails_Returns503NotAnEmpty200()
    {
        _factory.TechStacks.Failure = new FakeDbException("could not connect");

        var response = await _client.GetAsync("/api/v1/skill");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("database_unavailable", problem.GetProperty("code").GetString());
    }

    private static void Seed(PortfolioApiFactory factory)
    {
        factory.TechStacks.Failure = null;
        factory.TechStacks.TechStacks.Clear();
        factory.TechStacks.TechStacks.Add(
            new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend"));
    }
}
