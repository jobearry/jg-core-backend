using System.Net;
using System.Text.Json;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Features.Github;

namespace Portfolio.Api.Tests;

public class GithubClientServiceTests
{
    [Fact]
    public async Task GetRepositoriesByOwnerAsync_Owner_ReturnsAllRepositories()
    {
        const string responseBody =
            """[{"id":123,"name":"hello-world","full_name":"octocat/hello-world","private":false,"owner":{"login":"octocat","id":1}},{"id":124,"name":"secret","full_name":"octocat/secret","private":true,"owner":{"login":"octocat","id":1}}]""";
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody));
        var service = CreateService(handler);

        var repositories = await service.GetRepositoriesByOwnerAsync("octocat", CancellationToken.None);

        var repository = Assert.Single(repositories);
        Assert.Equal(123, repository.Id);
        Assert.Equal("hello-world", repository.Name);
        Assert.Equal("octocat", repository.Owner.Login);
        Assert.Equal(
            new Uri("https://api.github.test/users/octocat/repos?per_page=100&page=1"),
            Assert.Single(handler.RequestUris));
        Assert.Equal("Bearer", Assert.Single(handler.AuthorizationHeaders)!.Scheme);
        Assert.Equal("test-token", handler.AuthorizationHeaders[0]!.Parameter);
    }

    [Fact]
    public async Task GetRepositoriesByOwnerAsync_MoreThanOnePage_FetchesEveryPage()
    {
        var firstPage = JsonSerializer.Serialize(
            Enumerable.Range(1, 100).Select(id => new
            {
                id,
                name = $"repo-{id}",
                @private = false,
                owner = new { login = "octocat" }
            }));
        var handler = new QueuedHttpMessageHandler(
            (HttpStatusCode.OK, firstPage),
            (HttpStatusCode.OK, "[]"));
        var service = CreateService(handler);

        var repositories = await service.GetRepositoriesByOwnerAsync("octocat", CancellationToken.None);

        Assert.Equal(100, repositories.Count);
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Equal(
            new Uri("https://api.github.test/users/octocat/repos?per_page=100&page=2"),
            handler.RequestUris[1]);
    }

    [Fact]
    public async Task GetRepositoriesByOwnerAsync_FiltersToRequestedOwner()
    {
        const string responseBody =
            """[{"id":123,"name":"mine","private":false,"owner":{"login":"octocat"}},{"id":456,"name":"other","private":false,"owner":{"login":"someone-else"}}]""";
        var service = CreateService(new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody)));

        var repositories = await service.GetRepositoriesByOwnerAsync("octocat", CancellationToken.None);

        var repository = Assert.Single(repositories);
        Assert.Equal("mine", repository.Name);
    }

    [Fact]
    public async Task GetRepositoryLanguagesAsync_OwnerAndName_ReturnsLanguagesAndByteCounts()
    {
        var handler = new QueuedHttpMessageHandler(
            (HttpStatusCode.OK, """{"id":123,"name":"hello-world","private":false,"owner":{"login":"octocat"}}"""),
            (HttpStatusCode.OK, """{"C#":1234,"JavaScript":567}"""));
        var service = CreateService(handler);

        var languages = await service.GetRepositoryLanguagesAsync("octocat", "hello-world", CancellationToken.None);

        Assert.Equal(1234, languages["C#"]);
        Assert.Equal(567, languages["JavaScript"]);
        Assert.Equal(
            new Uri("https://api.github.test/repos/octocat/hello-world"),
            handler.RequestUris[0]);
        Assert.Equal(
            new Uri("https://api.github.test/repos/octocat/hello-world/languages"),
            handler.RequestUris[1]);
    }

    [Fact]
    public async Task GetRepositoryByOwnerAndIdAsync_Id_ReturnsRepository()
    {
        const string responseBody =
            """{"id":456,"name":"portfolio","full_name":"octocat/portfolio","private":false,"owner":{"login":"octocat","id":1}}""";
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody));
        var service = CreateService(handler);

        var repository = await service.GetRepositoryByOwnerAndIdAsync("octocat", 456, CancellationToken.None);

        Assert.Equal(456, repository.Id);
        Assert.Equal("portfolio", repository.Name);
        Assert.Equal(
            new Uri("https://api.github.test/repositories/456"),
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task GetRepositoryByOwnerAndIdAsync_OwnerDoesNotMatch_ThrowsNotFound()
    {
        const string responseBody =
            """{"id":456,"name":"portfolio","full_name":"other/portfolio","owner":{"login":"other","id":2}}""";
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetRepositoryByOwnerAndIdAsync("octocat", 456, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetRepositoryLanguagesAsync_PrivateRepository_ThrowsNotFound()
    {
        const string responseBody =
            """{"id":123,"name":"secret","private":true,"owner":{"login":"octocat"}}""";
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetRepositoryLanguagesAsync("octocat", "secret", CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Single(handler.RequestUris);
    }

    [Fact]
    public async Task GetRepositoryByOwnerAndIdAsync_PrivateRepository_ThrowsNotFound()
    {
        const string responseBody =
            """{"id":456,"name":"secret","private":true,"full_name":"octocat/secret","owner":{"login":"octocat","id":1}}""";
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.OK, responseBody));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetRepositoryByOwnerAndIdAsync("octocat", 456, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Single(handler.RequestUris);
    }

    private static GithubClientService CreateService(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            new GithubAPISettings
            {
                BaseUrl = "https://api.github.test/",
                Token = "test-token"
            });
}
