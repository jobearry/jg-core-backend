namespace Portfolio.Features.Project;

/// <summary>
/// The wire shape for adding a project. The experience link and tech-stack refs are
/// deliberately absent; they are owned by other slices (ADR-0003).
/// </summary>
public sealed record CreateProjectRequest(
    string ProjectName,
    string? Description,
    string? Contribution);
