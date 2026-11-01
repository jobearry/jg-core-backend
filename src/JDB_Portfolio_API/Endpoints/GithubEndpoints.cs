using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;

namespace JDB_Portfolio_API.Endpoints;

public static class GithubEndpoints
{
    public static void MapGithubEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet(
            "github/owner/{owner}/repositories",
            async (
                IGithubRepositoryService service,
                string owner,
                CancellationToken cancellationToken) =>
                (await service.GetRepositoriesByOwnerAsync(owner, cancellationToken)).ToHttpResult())
            .WithName("GetGithubRepositoriesByOwner")
            .WithTags("ExternalServices - GitHub")
            .WithSummary("Get all repositories for a GitHub owner")
            .WithDescription("Returns public repositories owned by the GitHub account for the specified owner.")
            .Produces<IReadOnlyList<GithubRepositoryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        routes.MapGet(
            "github/repository/{owner}/{repository}/languages",
            async (
                IGithubRepositoryService service,
                string owner,
                string repository,
                CancellationToken cancellationToken) =>
                (await service.GetRepositoryLanguagesAsync(owner, repository, cancellationToken)).ToHttpResult())
            .WithName("GetGithubRepositoryLanguages")
            .WithTags("ExternalServices - GitHub")
            .WithSummary("Get repository languages")
            .WithDescription("Returns language byte counts for a public repository.")
            .Produces<GithubRepositoryLanguagesDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        routes.MapGet(
            "github/owner/{owner}/repositories/{repositoryId:long}",
            async (
                IGithubRepositoryService service,
                string owner,
                long repositoryId,
                CancellationToken cancellationToken) =>
                (await service.GetRepositoryByOwnerAndIdAsync(owner, repositoryId, cancellationToken)).ToHttpResult())
            .WithName("GetGithubRepositoryByOwnerAndId")
            .WithTags("ExternalServices - GitHub")
            .WithSummary("Get a GitHub repository by owner and ID")
            .WithDescription("Fetches details for a public repository by numeric ID and returns not found if it belongs to a different owner.")
            .Produces<GithubRepositoryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
