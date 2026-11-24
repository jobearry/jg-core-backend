namespace Portfolio.Features.Project;

/// <summary>
/// The wire shape for replacing a project's tools. Only shared stack ids are accepted:
/// names, images and categories are resolved from the Skill read port, never from here.
/// An empty list clears the project's tools.
/// </summary>
public sealed record ReplaceProjectTechStacksRequest(IReadOnlyList<int>? TechStackIds);
