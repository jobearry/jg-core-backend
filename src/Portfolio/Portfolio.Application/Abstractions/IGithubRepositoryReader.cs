using Portfolio.Application.Models;

namespace Portfolio.Application.Abstractions;

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
