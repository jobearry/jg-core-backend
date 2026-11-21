namespace Portfolio.Features.Github;

/// <summary>
/// Bridges <see cref="IGithubRepositoryReader"/> to the GitHub client, keeping the
/// vendor DTOs inside the slice.
/// </summary>
public sealed class GithubRepositoryReader : IGithubRepositoryReader
{
    private readonly GithubClientService _client;

    public GithubRepositoryReader(GithubClientService client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<GithubRepository>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken) =>
        (await _client.GetRepositoriesByOwnerAsync(owner, cancellationToken))
            .Select(MapRepository)
            .ToList();

    public async Task<IReadOnlyDictionary<string, long>> GetRepositoryLanguagesAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken) =>
        await _client.GetRepositoryLanguagesAsync(owner, repository, cancellationToken);

    public async Task<GithubRepository> GetRepositoryByOwnerAndIdAsync(
        string owner,
        long repositoryId,
        CancellationToken cancellationToken)
    {
        var result = await _client.GetRepositoryByOwnerAndIdAsync(owner, repositoryId, cancellationToken);
        return MapRepository(result);
    }

    private static GithubRepository MapRepository(GithubRepoDto repository) =>
        new(
            repository.Id,
            repository.Name,
            repository.FullName,
            repository.Owner.Login,
            repository.HtmlUrl,
            repository.Description,
            repository.Language,
            repository.Topics);
}
