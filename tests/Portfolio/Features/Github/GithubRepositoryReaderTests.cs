using System.Net;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Features.Github;

namespace Portfolio.Api.Tests;

public class GithubRepositoryReaderTests
{
    [Fact]
    public async Task GetRepositoriesByOwnerAsync_MapsGitHubDtosToApplicationModels()
    {
        const string responseBody =
            """[{"id":123,"name":"hello-world","full_name":"octocat/hello-world","private":false,"owner":{"login":"octocat","id":1},"html_url":"https://github.com/octocat/hello-world","description":"Example","language":"C#","topics":["api"]}]""";
        var client = new GithubClientService(
            new HttpClient(new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody))),
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

}
