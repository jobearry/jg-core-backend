using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JDB_Portfolio_API.ExternalService.Github.Models;
using JDB_Portfolio_API.ExternalService.Github.Settings;

namespace JDB_Portfolio_API.ExternalService.Github.Services;

public sealed class GithubClientService
{
    private readonly HttpClient _httpClient;

    public GithubClientService(HttpClient httpClient, GithubAPISettings settings)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri($"{settings.BaseUrl.TrimEnd('/')}/");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JDB-Portfolio-API");

        if (!string.IsNullOrWhiteSpace(settings.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.Token);
        }
    }

    public async Task<IReadOnlyList<GithubRepoDto>> GetRepositoriesByOwnerAsync(
        string owner,
        CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var repositories = new List<GithubRepoDto>();

        for (var page = 1; ; page++)
        {
            var results = await GetAsync<List<GithubRepoDto>>(
                $"users/{Uri.EscapeDataString(owner)}/repos?per_page={pageSize}&page={page}",
                cancellationToken);

            repositories.AddRange(results.Where(repository =>
                IsPublic(repository) &&
                string.Equals(repository.Owner.Login, owner, StringComparison.OrdinalIgnoreCase)));
            if (results.Count < pageSize)
            {
                return repositories;
            }
        }
    }

    public Task<GithubRepoDto> GetRepositoryByOwnerAndIdAsync(
        string owner,
        long repositoryId,
        CancellationToken cancellationToken) =>
        GetRepositoryByIdAsync(repositoryId, owner, cancellationToken);

    public async Task<Dictionary<string, long>> GetRepositoryLanguagesAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        var repositoryDetails = await GetAsync<GithubRepoDto>(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}",
            cancellationToken);
        EnsurePublic(repositoryDetails);

        return await GetAsync<Dictionary<string, long>>(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/languages",
            cancellationToken);
    }

    private async Task<GithubRepoDto> GetRepositoryByIdAsync(
        long repositoryId,
        string? expectedOwner,
        CancellationToken cancellationToken)
    {
        var repository = await GetAsync<GithubRepoDto>($"repositories/{repositoryId}", cancellationToken);

        if (expectedOwner is not null &&
            !string.Equals(repository.Owner.Login, expectedOwner, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestException(
                $"GitHub repository '{repositoryId}' does not belong to owner '{expectedOwner}'.",
                inner: null,
                statusCode: HttpStatusCode.NotFound);
        }

        EnsurePublic(repository);
        return repository;
    }

    private static void EnsurePublic(GithubRepoDto repository)
    {
        if (!IsPublic(repository))
        {
            throw new HttpRequestException(
                "GitHub repository is not public.",
                inner: null,
                statusCode: HttpStatusCode.NotFound);
        }
    }

    private static bool IsPublic(GithubRepoDto repository) =>
        !(repository.Private ?? throw new InvalidOperationException(
            "GitHub repository visibility was missing."));

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GitHub returned {(int)response.StatusCode} for '{path}'.",
                inner: null,
                statusCode: response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return result ?? throw new InvalidOperationException($"GitHub returned an empty response for '{path}'.");
    }
}
