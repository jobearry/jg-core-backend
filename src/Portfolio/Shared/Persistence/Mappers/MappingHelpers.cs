using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Mappers;

internal static class MappingHelpers
{
    /// <summary>
    /// A blank role is deliberately undeclared, not an invalid one. The domain
    /// value object rejects blank strings, so absence is represented by null.
    /// </summary>
    internal static Role? ToRole(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new Role(value);

    /// <summary>
    /// The role columns are NOT NULL, so an undeclared role is written back as
    /// the empty string rather than null.
    /// </summary>
    internal static string ToStoredRole(Role? role) => role?.Value ?? string.Empty;

    internal static string? ToCategoryName(TechStackCategoryEntity? category) =>
        category is null ? null : category.Category;
}
