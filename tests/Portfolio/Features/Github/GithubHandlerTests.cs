using System.Net;
using Portfolio.Application.Common;
using Portfolio.Features.Github;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Application.Tests;

public sealed class GithubHandlerTests
{
    [Fact]
    public async Task GetRepositoryByOwnerAndIdAsync_WhenGitHubReturnsNotFound_ReturnsNotFoundError()
    {
        var handler = new GithubHandler(
            new NotFoundReader(),
            NullLogger<GithubHandler>.Instance);

        var result = await handler.GetRepositoryByOwnerAndIdAsync("octocat", 456, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("github_repository_not_found", result.Error.Code);
    }

    private sealed class NotFoundReader : IGithubRepositoryReader
    {
        public Task<IReadOnlyList<GithubRepository>> GetRepositoriesByOwnerAsync(
            string owner,
            CancellationToken cancellationToken) =>
            throw RepositoryNotFound();

        public Task<GithubRepository> GetRepositoryByOwnerAndIdAsync(
            string owner,
            long repositoryId,
            CancellationToken cancellationToken) =>
            throw RepositoryNotFound();

        public Task<IReadOnlyDictionary<string, long>> GetRepositoryLanguagesAsync(
            string owner,
            string repository,
            CancellationToken cancellationToken) =>
            throw RepositoryNotFound();

        private static HttpRequestException RepositoryNotFound() =>
            new("Not found", inner: null, HttpStatusCode.NotFound);
    }
}
