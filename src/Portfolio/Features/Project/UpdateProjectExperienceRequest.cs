namespace Portfolio.Features.Project;

/// <summary>
/// Updates only the project's experience link. No other fields are touched.
/// <c>null</c> detaches the project (matches <c>ON DELETE SET NULL</c>).
/// </summary>
public sealed record UpdateProjectExperienceRequest(
    int? ExperienceId);