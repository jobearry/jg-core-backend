using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Features.Overview;

namespace Portfolio.Application.Services;

public interface IGithubRepositoryService
{
    Task<Result<IReadOnlyList<GithubRepositoryDto>>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken);

    Task<Result<OverviewItemDto>> GetRepositoryCountAsync(
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
