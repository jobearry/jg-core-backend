using System.Text.Json.Serialization;

namespace Portfolio.Features.Github;

/// <summary>
/// The owner fields of GitHub's repository response that this API reads.
/// </summary>
public sealed class GithubOwnerDto
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("organizations_url")]
    public string OrganizationsUrl { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("user_view_type")]
    public string UserViewType { get; set; } = string.Empty;

    [JsonPropertyName("site_admin")]
    public bool SiteAdmin { get; set; }
}
