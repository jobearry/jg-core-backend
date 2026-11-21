using Portfolio.Endpoints;
using Portfolio.Features.Overview;

namespace Portfolio.Features.Github;

public static class GithubEndpoints
{
    public static void MapGithubEndpoints(
        this IEndpointRouteBuilder routes,
        string? rateLimitPolicy = null)
    {
        var repositories = routes.MapGet(
                "github/owner/{owner}/repositories",
                async (
                    GithubHandler handler,
                    string owner,
                    CancellationToken cancellationToken) =>
                    (await handler.GetRepositoriesByOwnerAsync(owner, cancellationToken)).ToHttpResult())
                .WithName("GetGithubRepositoriesByOwner")
                .WithTags("ExternalServices - GitHub")
                .WithSummary("Get all repositories for a GitHub owner")
                .WithDescription("Returns public repositories owned by the GitHub account for the specified owner.")
                .Produces<IReadOnlyList<GithubRepositoryDto>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status429TooManyRequests)
                .ProducesProblem(StatusCodes.Status502BadGateway)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

        var count = routes.MapGet(
                "github/owner/{owner}/repositories/count",
                async (
                    GithubHandler handler,
                    string owner,
                    CancellationToken cancellationToken) =>
                    (await handler.GetRepositoryCountAsync(owner, cancellationToken)).ToHttpResult())
                .WithName("GetGithubRepositoryCount")
                .WithTags("ExternalServices - GitHub")
                .WithSummary("Get the number of public repositories for a GitHub owner")
                .WithDescription("Returns the count of public repositories owned by the GitHub account.")
                .Produces<OverviewItemDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status429TooManyRequests)
                .ProducesProblem(StatusCodes.Status502BadGateway)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

        var languages = routes.MapGet(
                "github/repository/{owner}/{repository}/languages",
                async (
                    GithubHandler handler,
                    string owner,
                    string repository,
                    CancellationToken cancellationToken) =>
                    (await handler.GetRepositoryLanguagesAsync(owner, repository, cancellationToken)).ToHttpResult())
                .WithName("GetGithubRepositoryLanguages")
                .WithTags("ExternalServices - GitHub")
                .WithSummary("Get repository languages")
                .WithDescription("Returns language byte counts for a public repository.")
                .Produces<GithubRepositoryLanguagesDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status429TooManyRequests)
                .ProducesProblem(StatusCodes.Status502BadGateway)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

        var byId = routes.MapGet(
                "github/owner/{owner}/repositories/{repositoryId:long}",
                async (
                    GithubHandler handler,
                    string owner,
                    long repositoryId,
                    CancellationToken cancellationToken) =>
                    (await handler.GetRepositoryByOwnerAndIdAsync(owner, repositoryId, cancellationToken)).ToHttpResult())
                .WithName("GetGithubRepositoryByOwnerAndId")
                .WithTags("ExternalServices - GitHub")
                .WithSummary("Get a GitHub repository by owner and ID")
                .WithDescription("Fetches details for a public repository by owner and ID and returns not found if it belongs to a different owner.")
                .Produces<GithubRepositoryDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status429TooManyRequests)
                .ProducesProblem(StatusCodes.Status502BadGateway)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (rateLimitPolicy is not null)
        {
            repositories.RequireRateLimiting(rateLimitPolicy);
            count.RequireRateLimiting(rateLimitPolicy);
            languages.RequireRateLimiting(rateLimitPolicy);
            byId.RequireRateLimiting(rateLimitPolicy);
        }
    }
}
