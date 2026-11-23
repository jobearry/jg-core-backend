using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Portfolio.Domain.Entities;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract for Skill admin writes. Reads stay anonymous; every write needs the
/// Admin token and the admin rate-limit policy.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public class SkillAdminEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private const string SkillPath = "/api/v1/admin/skill";
    private const string CategoryPath = "/api/v1/admin/skill-category";

    private readonly JGBackendApiFactory _factory;

    public SkillAdminEndpointsTests(JGBackendApiFactory factory)
    {
        _factory = factory;
        Seed(factory);
    }

    [Fact]
    public async Task AdminCreateSkill_WithToken_Returns201AndLocation()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(SkillPath, new
        {
            stackName = "F#",
            imgSrc = (string?)null,
            category = "Backend"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/skill/3", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("F#", body.GetProperty("stackName").GetString());
        Assert.Equal("Backend", body.GetProperty("category").GetString());
    }

    [Fact]
    public async Task AdminCreateSkill_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(SkillPath, new
        {
            stackName = "F#",
            imgSrc = (string?)null,
            category = "Backend"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateSkill_WithBlankName_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(SkillPath, new
        {
            stackName = "   ",
            imgSrc = (string?)null,
            category = "Backend"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminCreateSkill_WithUnknownCategory_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(SkillPath, new
        {
            stackName = "F#",
            imgSrc = (string?)null,
            category = "Unknown"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminUpdateSkill_WithToken_Returns200AndKeepsTheId()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{SkillPath}/2", new
        {
            stackName = "C# 12",
            imgSrc = "new.svg",
            category = "Backend"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("id").GetInt32());
        Assert.Equal("C# 12", body.GetProperty("stackName").GetString());
        Assert.Equal("new.svg", body.GetProperty("imgSrc").GetString());
    }

    [Fact]
    public async Task AdminUpdateSkill_ForMissingId_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{SkillPath}/99", new
        {
            stackName = "C# 12",
            imgSrc = (string?)null,
            category = "Backend"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteSkill_WithToken_Returns204()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{SkillPath}/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.TechStacks.TechStacks);
    }

    [Fact]
    public async Task AdminDeleteSkill_WhenCitedByAProject_Returns409()
    {
        _factory.TechStacks.IsUsedByProjects = true;
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{SkillPath}/2");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(_factory.TechStacks.TechStacks);
    }

    [Fact]
    public async Task AdminDeleteSkill_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{SkillPath}/2");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateCategory_WithToken_Returns201AndLocation()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            CategoryPath, new { category = "Frontend" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/skill-category/2", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Frontend", body.GetProperty("category").GetString());
    }

    [Fact]
    public async Task AdminCreateCategory_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CategoryPath, new { category = "Frontend" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateCategory_WithNullBody_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            CategoryPath, new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminUpdateCategory_WithToken_Returns200()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync(
            $"{CategoryPath}/1", new { category = "Back-end" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("id").GetInt32());
        Assert.Equal("Back-end", body.GetProperty("category").GetString());
    }

    [Fact]
    public async Task AdminDeleteCategory_WhenItHasStacks_Returns409()
    {
        _factory.SkillCategories.HasStacks = true;
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{CategoryPath}/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(_factory.SkillCategories.Categories);
    }

    [Fact]
    public async Task AdminDeleteCategory_WhenEmpty_Returns204()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{CategoryPath}/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.SkillCategories.Categories);
    }

    private static void Seed(JGBackendApiFactory factory)
    {
        factory.TechStacks.Failure = null;
        factory.TechStacks.IsUsedByProjects = false;
        factory.TechStacks.TechStacks.Clear();
        factory.TechStacks.TechStacks.Add(new TechStack(2, "C#", "csharp.svg", "Backend"));

        factory.SkillCategories.Failure = null;
        factory.SkillCategories.HasStacks = false;
        factory.SkillCategories.Categories.Clear();
        factory.SkillCategories.Categories.Add(new TechStackCategory(1, "Backend"));
    }
}
