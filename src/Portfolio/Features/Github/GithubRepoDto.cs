using System.Text.Json.Serialization;

namespace Portfolio.Features.Github;

/// <summary>
/// The subset of GitHub's repository response this API reads.
/// </summary>
public sealed class GithubRepoDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("private")]
    public bool? Private { get; set; }

    [JsonPropertyName("owner")]
    public GithubOwnerDto Owner { get; set; } = new();

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("languages_url")]
    public string LanguagesUrl { get; set; } = string.Empty;

    [JsonPropertyName("commits_url")]
    public string CommitsUrl { get; set; } = string.Empty;

    [JsonPropertyName("git_commits_url")]
    public string GitCommitsUrl { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }

    [JsonPropertyName("watchers_count")]
    public int WatchersCount { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = [];

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = string.Empty;

    [JsonPropertyName("watchers")]
    public int Watchers { get; set; }

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = string.Empty;
}
