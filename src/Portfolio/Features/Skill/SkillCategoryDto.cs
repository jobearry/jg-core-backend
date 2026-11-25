namespace Portfolio.Features.Skill;

/// <summary>
/// The published shape of a tech stack category. It is returned by the category
/// collection and detail reads, and by admin category writes.
/// </summary>
public sealed record SkillCategoryDto(int Id, string Category);
