namespace Core.Infrastructure;

/// <summary>
/// Names of rate-limiting policies defined by the host and applied to endpoints.
/// </summary>
public static class RateLimitPolicies
{
    public const string AuthLogin = "auth-login";

    /// <summary>Notion-backed routes; each upstream page call costs a Notion request.</summary>
    public const string NotionPassthrough = "notion-passthrough";

    /// <summary>GitHub-backed routes; each call costs a GitHub request.</summary>
    public const string GithubPassthrough = "github-passthrough";
}
