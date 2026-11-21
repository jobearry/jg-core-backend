namespace Portfolio.Features.Github;

/// <summary>
/// GitHub credentials and base URL, bound from configuration.
/// </summary>
public class GithubAPISettings
{
    public string Token { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.github.com";
}
