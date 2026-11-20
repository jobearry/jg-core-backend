namespace Portfolio.Features.Github;

/// <summary>
/// A public repository as read from GitHub, before it is mapped to the wire DTO.
/// </summary>
public sealed record GithubRepository(
    long Id,
    string Name,
    string FullName,
    string OwnerLogin,
    string HtmlUrl,
    string? Description,
    string? Language,
    IReadOnlyList<string> Topics);
