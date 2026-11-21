namespace Portfolio.Features.Github;

/// <summary>
/// Reads public repository data from GitHub.
/// </summary>
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
