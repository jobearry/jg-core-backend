using System.Net;
using System.Text;
using JDB_Portfolio_API.Adapters;
using JDB_Portfolio_API.Application.Abstractions;
using JDB_Portfolio_API.Application.Models;
using JDB_Portfolio_API.ExternalService.Github.Services;
using JDB_Portfolio_API.ExternalService.Github.Settings;

namespace JDB_Portfolio_API.Api.Tests;

public class GithubRepositoryReaderTests
{
    [Fact]
    public async Task GetRepositoriesByOwnerAsync_MapsGitHubDtosToApplicationModels()
    {
        const string responseBody =
            """[{"id":123,"name":"hello-world","full_name":"octocat/hello-world","private":false,"owner":{"login":"octocat","id":1},"html_url":"https://github.com/octocat/hello-world","description":"Example","language":"C#","topics":["api"]}]""";
        var client = new GithubClientService(
            new HttpClient(new StubHandler(responseBody)),
            new GithubAPISettings { BaseUrl = "https://api.github.test/" });
        IGithubRepositoryReader reader = new GithubRepositoryReader(client);

        var repository = Assert.Single(
            await reader.GetRepositoriesByOwnerAsync("octocat", CancellationToken.None));

        Assert.Equal(123, repository.Id);
        Assert.Equal("hello-world", repository.Name);
        Assert.Equal("octocat/hello-world", repository.FullName);
        Assert.Equal("octocat", repository.OwnerLogin);
        Assert.Equal("https://github.com/octocat/hello-world", repository.HtmlUrl);
        Assert.Equal("Example", repository.Description);
        Assert.Equal("C#", repository.Language);
        Assert.Equal(["api"], repository.Topics);
    }

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
    }
}
