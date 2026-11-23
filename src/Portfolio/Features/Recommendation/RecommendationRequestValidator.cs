using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Features.Recommendation;

/// <summary>
/// Validates admin input before the domain constructor or EF conventions can turn a
/// bad request into a 500. Person name is capped at the column length; career position
/// is capped by the <see cref="Role"/> value object.
/// </summary>
internal static class RecommendationRequestValidator
{
    private const int PersonNameMaxLength = 50;

    public static Error? Validate(string? personName, string? description, string? careerPosition)
    {
        if (string.IsNullOrWhiteSpace(personName))
        {
            return Error.Validation("Person name is required.");
        }

        if (personName.Length > PersonNameMaxLength)
        {
            return Error.Validation($"Person name cannot exceed {PersonNameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Description is required.");
        }

        if (careerPosition is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(careerPosition))
        {
            return Error.Validation("Career position cannot be blank.");
        }

        return careerPosition.Length > Role.MaxLength
            ? Error.Validation($"Career position cannot exceed {Role.MaxLength} characters.")
            : null;
    }
}
