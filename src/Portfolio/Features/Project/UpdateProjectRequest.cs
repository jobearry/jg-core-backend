namespace Portfolio.Features.Project;

/// <summary>
/// The wire shape for replacing a project's editable fields.
/// </summary>
public sealed record UpdateProjectRequest(
    string ProjectName,
    string? Description,
    string? Contribution);
