namespace Portfolio.Application.Models;

public sealed record GithubRepository(
    long Id,
    string Name,
    string FullName,
    string OwnerLogin,
    string HtmlUrl,
    string? Description,
    string? Language,
    IReadOnlyList<string> Topics);
