using System.Net;
using System.Text.Json;
using JDB_Portfolio_API.Application.Abstractions;
using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Models;
using Microsoft.Extensions.Logging;

namespace JDB_Portfolio_API.Application.Services;

public sealed class GithubRepositoryService : IGithubRepositoryService
{
    private readonly IGithubRepositoryReader _reader;
    private readonly ILogger<GithubRepositoryService> _logger;

    public GithubRepositoryService(
        IGithubRepositoryReader reader,
        ILogger<GithubRepositoryService> logger)
    {
        _reader = reader;
        _logger = logger;
    }

    public Task<Result<IReadOnlyList<GithubRepositoryDto>>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<GithubRepositoryDto>>(
            async ct => (await _reader.GetRepositoriesByOwnerAsync(owner, ct))
                .Select(Map)
                .ToList(),
            "fetch repositories for the GitHub owner",
            cancellationToken);

    public Task<Result<GithubRepositoryDto>> GetRepositoryByOwnerAndIdAsync(
        string owner,
        long repositoryId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async ct => Map(await _reader.GetRepositoryByOwnerAndIdAsync(owner, repositoryId, ct)),
            "fetch the GitHub repository",
            cancellationToken);

    public Task<Result<GithubRepositoryLanguagesDto>> GetRepositoryLanguagesAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async ct => new GithubRepositoryLanguagesDto(
                await _reader.GetRepositoryLanguagesAsync(owner, repository, ct)),
            "fetch GitHub repository languages",
            cancellationToken);

    private async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result<T>.Success(await operation(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var error = exception switch
            {
                HttpRequestException { StatusCode: HttpStatusCode.NotFound } => Error.NotFound(
                    "The requested GitHub repository was not found.",
                    "github_repository_not_found"),
                HttpRequestException => Error.Upstream(
                    $"Failed to {operationDescription}. Please try again later.",
                    "github_unavailable"),
                InvalidOperationException or JsonException => Error.Upstream(
                    $"Failed to {operationDescription}: the response could not be read.",
                    "github_invalid_response"),
                _ => Error.Unexpected($"Failed to {operationDescription}.")
            };

            if (error.Kind == ErrorKind.Unexpected)
            {
                _logger.LogError(
                    exception,
                    "Unexpected failure while trying to {Operation} ({ErrorCode}).",
                    operationDescription,
                    error.Code);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Dependency failure while trying to {Operation} ({ErrorCode}).",
                    operationDescription,
                    error.Code);
            }

            return Result<T>.Failure(error);
        }
    }

    private static GithubRepositoryDto Map(GithubRepository repository) =>
        new(
            repository.Id,
            repository.Name,
            repository.FullName,
            repository.OwnerLogin,
            repository.HtmlUrl,
            repository.Description,
            repository.Language,
            repository.Topics);
}
