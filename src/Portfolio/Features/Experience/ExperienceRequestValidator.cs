using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Features.Experience;

/// <summary>
/// Validates admin input before the value objects or PostgreSQL can turn a bad
/// request into a 500. This duplicates their limits deliberately: application code
/// cannot reference the EF column configuration, but it must answer 400 first.
/// </summary>
internal static class ExperienceRequestValidator
{
    // Mirrors ExperienceEntityConfiguration: the type column is varchar(20).
    private const int TypeMaxLength = 20;

    public static Error? Validate(
        string? company,
        DateTime? startedAt,
        DateTime? finishedAt,
        string? role,
        string? type)
    {
        if (string.IsNullOrWhiteSpace(company))
        {
            return Error.Validation("Company is required.");
        }

        if (company.Length > Company.MaxLength)
        {
            return Error.Validation($"Company cannot exceed {Company.MaxLength} characters.");
        }

        if (!string.IsNullOrWhiteSpace(role) && role.Length > Role.MaxLength)
        {
            return Error.Validation($"Role cannot exceed {Role.MaxLength} characters.");
        }

        if (type is not null && type.Length > TypeMaxLength)
        {
            return Error.Validation($"Type cannot exceed {TypeMaxLength} characters.");
        }

        if (startedAt.HasValue && startedAt.Value == default)
        {
            return Error.Validation("StartedAt must be a valid date.");
        }

        if (finishedAt.HasValue && finishedAt.Value == default)
        {
            return Error.Validation("FinishedAt must be a valid date.");
        }

        if (startedAt.HasValue && startedAt.Value.Date > DateTime.UtcNow.Date)
        {
            return Error.Validation("A role cannot start in the future.");
        }

        return finishedAt < startedAt
            ? Error.Validation("FinishedAt cannot be before StartedAt.")
            : null;
    }

    /// <summary>Blank input means the role is deliberately undeclared.</summary>
    public static Role? ToRole(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new Role(value);
}
