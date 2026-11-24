namespace Portfolio.Features.Experience;

/// <summary>
/// The wire shape for replacing a role's editable fields. The identity and the
/// Projects projection are not part of this command.
/// </summary>
public sealed record UpdateExperienceRequest(
    string Company,
    string? Role,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    string? Description,
    string? Responsibility,
    string? Type);
