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
[Collection(JGBackendHostCollection.Name)]
public class SkillEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private readonly JGBackendApiFactory _factory;
    private readonly HttpClient _client;

    public SkillEndpointsTests(JGBackendApiFactory factory)
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

    [Fact]
    public async Task GetSkillById_WhenFound_ReturnsTheDto()
    {
        var response = await _client.GetAsync("/api/v1/skill/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("id").GetInt32());
        Assert.Equal("C#", json.GetProperty("stackName").GetString());
        Assert.Equal("Backend", json.GetProperty("category").GetString());
    }

    [Fact]
    public async Task GetSkillById_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/skill/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSkillCategoryById_WhenFound_ReturnsTheDto()
    {
        var response = await _client.GetAsync("/api/v1/skill-category/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("id").GetInt32());
        Assert.Equal("Backend", json.GetProperty("category").GetString());
    }

    [Fact]
    public async Task GetSkillCategoryById_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/skill-category/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void Seed(JGBackendApiFactory factory)
    {
        factory.TechStacks.Failure = null;
        factory.TechStacks.TechStacks.Clear();
        factory.TechStacks.TechStacks.Add(
            new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend"));

        factory.SkillCategories.Failure = null;
        factory.SkillCategories.Categories.Clear();
        factory.SkillCategories.Categories.Add(new TechStackCategory(1, "Backend"));
    }
}
