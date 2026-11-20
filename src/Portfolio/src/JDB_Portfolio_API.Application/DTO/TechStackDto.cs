namespace JDB_Portfolio_API.Application.DTO;

/// <summary>
/// The published shape of a tech stack. <c>CreatedAt</c> is deliberately absent:
/// it is a persistence audit column that no client asked for (plan 2.1).
/// </summary>
public sealed record TechStackDto(
    int Id,
    string StackName,
    string? ImgSrc,
    string? Category);
