namespace Portfolio.Features.Experience;

/// <summary>
/// The wire shape for adding a role. Every scalar field is present; the read-only
/// Projects projection is not part of this command (ADR-0003).
/// </summary>
public sealed record CreateExperienceRequest(
    string Company,
    string? Role,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    string? Description,
    string? Responsibility,
    string? Type);
