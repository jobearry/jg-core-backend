using Portfolio.Application.Common;

namespace Portfolio.Features.Project;

/// <summary>
/// Admin input guards. The project columns are unbounded <c>text</c>, so unlike the
/// recommendation and skill writes there is no column length to enforce here.
/// </summary>
internal static class ProjectRequestValidator
{
    public static Error? Validate(string? projectName) =>
        string.IsNullOrWhiteSpace(projectName)
            ? Error.Validation("Project name is required.")
            : null;
}
