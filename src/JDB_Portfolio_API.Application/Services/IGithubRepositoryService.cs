using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;

namespace JDB_Portfolio_API.Application.Services;

public interface IGithubRepositoryService
{
    Task<Result<IReadOnlyList<GithubRepositoryDto>>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken);

    Task<Result<GithubRepositoryDto>> GetRepositoryByOwnerAndIdAsync(
        string owner,
        long repositoryId,
        CancellationToken cancellationToken);

    Task<Result<GithubRepositoryLanguagesDto>> GetRepositoryLanguagesAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken);

}
