namespace Portfolio.Features.Project;

/// <summary>
/// The wire shape for removing a single tech stack link from a project.
/// Only the shared stack id is required.
/// </summary>
public sealed record RemoveProjectTechStackRequest(int TechStackId);