namespace Portfolio.Application.DTO;

/// <summary>
/// The published shape of a project. <c>ExperienceId</c> is deliberately absent:
/// a project is its own aggregate (ADR-0003, decision D3), and its internal link
/// back to the role is not part of the contract.
/// </summary>
public sealed record ProjectDto(
    int Id,
    string ProjectName,
    string? Description,
    string? Contribution,
    IReadOnlyList<TechStackDto> TechStacks);
