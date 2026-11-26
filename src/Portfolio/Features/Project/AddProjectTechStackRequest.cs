namespace Portfolio.Features.Project;

/// <summary>
/// The wire shape for adding a single tech stack link to a project.
/// Only the shared stack id is accepted; the snapshot is resolved from the Skill read port.
/// </summary>
public sealed record AddProjectTechStackRequest(int TechStackId);