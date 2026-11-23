namespace Portfolio.Features.Skill;

/// <summary>
/// The published shape of a tech stack category. Categories have no public collection
/// endpoint today; the detail GET exists so a created category's Location is followable.
/// </summary>
public sealed record SkillCategoryDto(int Id, string Category);
