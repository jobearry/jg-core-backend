namespace Portfolio.Features.Skill;

/// <summary>
/// The wire shape for adding a tech stack. <c>Category</c> is the name of an existing
/// category; the handler resolves it to the id the row needs.
/// </summary>
public sealed record CreateSkillRequest(
    string StackName,
    string? ImgSrc,
    string Category);
