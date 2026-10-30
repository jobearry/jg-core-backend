using System.Net;
using System.Text;
using System.Text.Json;
using JDB_Portfolio_API.ExternalService.Github.Services;
using JDB_Portfolio_API.ExternalService.Github.Settings;

namespace JDB_Portfolio_API.Api.Tests;

public class GithubClientServiceTests
{
    [Fact]
    public async Task GetRepositoriesByOwnerAsync_Owner_ReturnsAllRepositories()
    {
        const string responseBody =
            """[{"id":123,"name":"hello-world","full_name":"octocat/hello-world","owner":{"login":"octocat","id":1}}]""";
        var handler = new StubHandler(responseBody);
        var service = CreateService(handler);

        var repositories = await service.GetRepositoriesByOwnerAsync("octocat", CancellationToken.None);

        var repository = Assert.Single(repositories);
        Assert.Equal(123, repository.Id);
        Assert.Equal("hello-world", repository.Name);
        Assert.Equal("octocat", repository.Owner.Login);
        Assert.Equal(
            new Uri("https://api.github.test/users/octocat/repos?per_page=100&page=1"),
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task GetRepositoriesByOwnerAsync_MoreThanOnePage_FetchesEveryPage()
    {
        var firstPage = JsonSerializer.Serialize(
            Enumerable.Range(1, 100).Select(id => new
            {
                id,
                name = $"repo-{id}",
                owner = new { login = "octocat" }
            }));
        var handler = new StubHandler(firstPage, "[]");
        var service = CreateService(handler);

        var repositories = await service.GetRepositoriesByOwnerAsync("octocat", CancellationToken.None);

        Assert.Equal(100, repositories.Count);
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Equal(
            new Uri("https://api.github.test/users/octocat/repos?per_page=100&page=2"),
            handler.RequestUris[1]);
    }

    [Fact]
    public async Task GetRepositoryLanguagesAsync_OwnerAndName_ReturnsLanguagesAndByteCounts()
    {
        var handler = new StubHandler("""{"C#":1234,"JavaScript":567}""");
        var service = CreateService(handler);

        var languages = await service.GetRepositoryLanguagesAsync("octocat", "hello-world", CancellationToken.None);

        Assert.Equal(1234, languages["C#"]);
        Assert.Equal(567, languages["JavaScript"]);
        Assert.Equal(
            new Uri("https://api.github.test/repos/octocat/hello-world/languages"),
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task GetRepositoryByOwnerAndIdAsync_Id_ReturnsRepository()
    {
        const string responseBody =
            """{"id":456,"name":"portfolio","full_name":"octocat/portfolio","owner":{"login":"octocat","id":1}}""";
        var handler = new StubHandler(responseBody);
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
        var handler = new StubHandler(responseBody);
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetRepositoryByOwnerAndIdAsync("octocat", 456, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    private static GithubClientService CreateService(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            new GithubAPISettings
            {
                BaseUrl = "https://api.github.test/"
            });

    private sealed class StubHandler(params string[] responseBodies) : HttpMessageHandler
    {
        private readonly Queue<string> _responseBodies = new(responseBodies);

        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBodies.Dequeue(), Encoding.UTF8, "application/json")
            });
        }
    }
}
