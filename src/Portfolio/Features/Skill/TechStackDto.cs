namespace Portfolio.Features.Skill;

/// <summary>
/// The published shape of a tech stack. <c>CreatedAt</c> is deliberately absent:
/// it is a persistence audit column that no client asked for (plan 2.1).
/// The type name stays <c>TechStackDto</c> because it is also the nested shape of a
/// project's tools and is part of the published Swagger schema names.
/// </summary>
public sealed record TechStackDto(
    int Id,
    string StackName,
    string? ImgSrc,
    string? Category);
