namespace Portfolio.Features.Skill;

/// <summary>
/// The wire shape for replacing a tech stack's editable fields.
/// </summary>
public sealed record UpdateSkillRequest(
    string StackName,
    string? ImgSrc,
    string Category);
