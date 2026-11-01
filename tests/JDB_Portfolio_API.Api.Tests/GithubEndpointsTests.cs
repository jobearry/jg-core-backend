using System.Net;
using System.Text.Json;

namespace JDB_Portfolio_API.Api.Tests;

public sealed class GithubEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly HttpClient _client;

    public GithubEndpointsTests(PortfolioApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRepositoriesByOwner_ReturnsAllOwnerRepositoriesWithoutApiToken()
    {
        var response = await _client.GetAsync("/api/v1/github/owner/octocat/repositories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Equal("hello-world", json.RootElement[0].GetProperty("name").GetString());
        Assert.Equal("portfolio", json.RootElement[1].GetProperty("name").GetString());
        Assert.Equal("octocat", json.RootElement[0].GetProperty("ownerLogin").GetString());
    }

    [Fact]
    public async Task GetRepositoryLanguages_ReturnsLanguageByteCountsWithoutApiToken()
    {
        var response = await _client.GetAsync(
            "/api/v1/github/repository/octocat/hello-world/languages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var languages = json.RootElement.GetProperty("languages");
        Assert.Equal(1234, languages.GetProperty("C#").GetInt64());
        Assert.Equal(567, languages.GetProperty("JavaScript").GetInt64());
    }

    [Fact]
    public async Task GetRepositoryByOwnerAndId_ReturnsRepositoryDtoWithoutApiToken()
    {
        var response = await _client.GetAsync("/api/v1/github/owner/octocat/repositories/456");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(456, json.RootElement.GetProperty("id").GetInt64());
        Assert.Equal("portfolio", json.RootElement.GetProperty("name").GetString());
    }
}
