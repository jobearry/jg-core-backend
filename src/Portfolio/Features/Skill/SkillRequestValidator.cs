using Portfolio.Application.Common;

namespace Portfolio.Features.Skill;

/// <summary>
/// Admin input guards. The column caps live here so bad input becomes
/// <c>400 + validation_failed</c> instead of a repository exception.
/// </summary>
internal static class SkillRequestValidator
{
    private const int MaxLength = 255;

    public static Error? ValidateStack(string? stackName, string? imgSrc, string? category)
    {
        if (string.IsNullOrWhiteSpace(stackName))
            return Error.Validation("Stack name is required.");

        if (stackName.Length > MaxLength)
            return Error.Validation($"Stack name cannot exceed {MaxLength} characters.");

        if (imgSrc is { Length: > MaxLength })
            return Error.Validation($"Image source cannot exceed {MaxLength} characters.");

        return ValidateCategoryName(category);
    }

    public static Error? ValidateCategory(string? category) => ValidateCategoryName(category);

    private static Error? ValidateCategoryName(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Error.Validation("Category is required.");

        return category.Length > MaxLength
            ? Error.Validation($"Category cannot exceed {MaxLength} characters.")
            : null;
    }
}
