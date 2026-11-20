namespace Portfolio.Features.Github;

/// <summary>Repository information returned by the GitHub repository endpoints.</summary>
public sealed record GithubRepositoryDto(
    long Id,
    string Name,
    string FullName,
    string OwnerLogin,
    string HtmlUrl,
    string? Description,
    string? Language,
    IReadOnlyList<string> Topics);

/// <summary>Repository language names and their byte counts.</summary>
public sealed record GithubRepositoryLanguagesDto(IReadOnlyDictionary<string, long> Languages);
