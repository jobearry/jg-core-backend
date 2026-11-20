using Portfolio.Application.Abstractions;
using Portfolio.Application.Models;

namespace Portfolio.Api.Tests.Fakes;

public sealed class FakeGithubRepositoryReader : IGithubRepositoryReader
{
    public Task<IReadOnlyList<GithubRepository>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GithubRepository>>(
            [Repository(owner, "hello-world"), Repository(owner, "portfolio", 456)]);

    public Task<GithubRepository> GetRepositoryByOwnerAndIdAsync(
        string owner,
        long repositoryId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Repository(owner, "portfolio", repositoryId));

    public Task<IReadOnlyDictionary<string, long>> GetRepositoryLanguagesAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, long> languages = new Dictionary<string, long>
        {
            ["C#"] = 1234,
            ["JavaScript"] = 567
        };
        return Task.FromResult(languages);
    }

    private static GithubRepository Repository(string owner, string repository, long id = 123) =>
        new(
            id,
            repository,
            $"{owner}/{repository}",
            owner,
            $"https://github.com/{owner}/{repository}",
            "Example repository",
            "C#",
            ["api"]);
}
