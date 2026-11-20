using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract tests for the Project slice. The wire shape deliberately omits
/// the aggregate's internal link back to an experience.
/// </summary>
public class ProjectEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly HttpClient _client;

    public ProjectEndpointsTests(PortfolioApiFactory factory)
    {
        _client = factory.CreateClient();
        Seed(factory);
    }

    [Fact]
    public async Task GetProject_DoesNotExposeExperienceIdOrPeriod()
    {
        var response = await _client.GetAsync("/api/v1/project");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var project = Assert.Single(json.EnumerateArray());

        Assert.Equal("Portfolio API", project.GetProperty("projectName").GetString());
        Assert.False(project.TryGetProperty("experienceId", out _));
        Assert.False(project.TryGetProperty("period", out _));
    }

    private static void Seed(PortfolioApiFactory factory)
    {
        factory.Projects.Projects.Clear();
        factory.Projects.Projects.Add(new Project(
            7,
            null,
            "Portfolio API",
            "The API behind the site",
            "Everything",
            new[] { new TechStackRef(2, "C#", "https://example.test/csharp.svg", "Backend") }));
    }
}
