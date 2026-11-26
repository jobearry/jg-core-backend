using Portfolio.Application.Common;

namespace Portfolio.Features.Project;

/// <summary>
/// Guards the join write. Ids must be positive and unique so a malformed body becomes
/// 400 rather than a database constraint error. An empty list is a valid "clear all".
/// </summary>
internal static class ProjectTechStackRequestValidator
{
    public static Error? Validate(IReadOnlyList<int>? techStackIds)
    {
        if (techStackIds is null)
        {
            return Error.Validation("A list of tech stack ids is required.");
        }

        if (techStackIds.Any(id => id <= 0))
        {
            return Error.Validation("Tech stack ids must be positive.");
        }

        return techStackIds.Count != techStackIds.Distinct().Count()
            ? Error.Validation("Tech stack ids must not contain duplicates.")
            : null;
    }
}
