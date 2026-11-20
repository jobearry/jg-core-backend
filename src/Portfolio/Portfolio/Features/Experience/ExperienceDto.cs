using Portfolio.Application.DTO;

namespace Portfolio.Features.Experience;

/// <summary>
/// The published shape of a role held at a company.
///
/// <para>
/// <c>Company</c> and <c>Role</c> are flattened to strings instead of the domain's
/// value objects, so the wire no longer carries <c>{ "value": … }</c> wrappers.
/// <c>Role</c> is nullable: <c>null</c> means the role is deliberately undeclared.
/// <c>Company</c> is not nullable: a role always names a company (plan 3.1a).
/// </para>
///
/// <para>
/// <c>DurationMonths</c> replaces the old <c>duration</c> field, which serialized a
/// <see cref="TimeSpan"/> as <c>"1231.05:06:07"</c>. It is whole calendar months
/// between the start and the end — or between the start and now, while the role is
/// ongoing — and <c>null</c> when the start date is unknown. Formatting it for
/// display is the client's job; English plurals do not belong in a transport contract.
/// </para>
/// </summary>
public sealed record ExperienceDto(
    int Id,
    string Company,
    string? Role,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? DurationMonths,
    string? Description,
    string? Responsibility,
    string? Type,
    IReadOnlyList<ProjectDto> Projects);
