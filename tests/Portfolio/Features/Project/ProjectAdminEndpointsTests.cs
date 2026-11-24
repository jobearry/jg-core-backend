using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract for Project admin writes. Reads stay anonymous; every write needs
/// the Admin token. Experience and tech-stack relationships are out of this slice.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public class ProjectAdminEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private const string ProjectPath = "/api/v1/admin/project";

    private readonly JGBackendApiFactory _factory;

    public ProjectAdminEndpointsTests(JGBackendApiFactory factory)
    {
        _factory = factory;
        Seed(factory);
    }

    [Fact]
    public async Task AdminCreateProject_WithToken_Returns201AndLocation()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(ProjectPath, new
        {
            projectName = "New project",
            description = "A description",
            contribution = "Mine"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/project/8", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New project", body.GetProperty("projectName").GetString());
        Assert.Empty(body.GetProperty("techStacks").EnumerateArray());
    }

    [Fact]
    public async Task AdminCreateProject_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(ProjectPath, new
        {
            projectName = "New project",
            description = (string?)null,
            contribution = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateProject_WithBlankName_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(ProjectPath, new
        {
            projectName = "  ",
            description = (string?)null,
            contribution = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminCreateProject_WithNullBody_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            ProjectPath, new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminUpdateProject_WithToken_Returns200AndKeepsTheId()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{ProjectPath}/7", new
        {
            projectName = "Portfolio API v2",
            description = "New description",
            contribution = "New contribution"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, body.GetProperty("id").GetInt32());
        Assert.Equal("Portfolio API v2", body.GetProperty("projectName").GetString());
        Assert.Equal("New description", body.GetProperty("description").GetString());
    }

    [Fact]
    public async Task AdminUpdateProject_ForMissingId_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{ProjectPath}/99", new
        {
            projectName = "Portfolio API v2",
            description = (string?)null,
            contribution = (string?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteProject_WithToken_Returns204()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{ProjectPath}/7");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.Projects.Projects);
    }

    [Fact]
    public async Task AdminDeleteProject_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{ProjectPath}/7");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static void Seed(JGBackendApiFactory factory)
    {
        factory.Projects.Failure = null;
        factory.Projects.Projects.Clear();
        factory.Projects.Projects.Add(new Project(
            7,
            null,
            "Portfolio API",
            "The API behind the site",
            "Everything",
            new[] { new TechStackRef(2, "C#", null, "Backend") }));
    }
}
