using JDB_Portfolio_API.Application.Models;

namespace JDB_Portfolio_API.Application.Abstractions;

public interface IGithubRepositoryReader
{
    Task<IReadOnlyList<GithubRepository>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken);

    Task<GithubRepository> GetRepositoryByOwnerAndIdAsync(
        string owner,
        long repositoryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, long>> GetRepositoryLanguagesAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken);

}
