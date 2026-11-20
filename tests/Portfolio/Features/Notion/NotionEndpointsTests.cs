using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Features.Notion;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract tests for the Notion slice. An upstream outage and an upstream
/// format change share a status code but carry different error codes.
/// </summary>
public class NotionEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly PortfolioApiFactory _factory;
    private readonly HttpClient _client;

    public NotionEndpointsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        Seed(factory);
    }

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

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>())!;
    }

    private static async Task<JsonElement> GetProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    private static void Seed(PortfolioApiFactory factory)
    {
        factory.Notion.Failure = null;
        factory.Notion.Pages.Clear();
        factory.Notion.Pages.Add(new PageContent(
            "https://notion.so/page",
            "2026-06-27",
            "About me"));
    }
}
