using System.Net;
using JDB_Portfolio_API.Application.Abstractions;
using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.Models;
using JDB_Portfolio_API.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace JDB_Portfolio_API.Application.Tests;

public sealed class GithubRepositoryServiceTests
{
    [Fact]
    public async Task GetRepositoryByOwnerAndIdAsync_WhenGitHubReturnsNotFound_ReturnsNotFoundError()
    {
        var service = new GithubRepositoryService(
            new NotFoundReader(),
            NullLogger<GithubRepositoryService>.Instance);

        var result = await service.GetRepositoryByOwnerAndIdAsync("octocat", 456, CancellationToken.None);

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
