using System;
using System.Collections.Generic;
using System.Linq;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Work delivered, optionally attached to an experience.
/// </summary>
/// <remarks>
/// Plan 3.3 gave this entity a constructor that validates and took away the public
/// setters: before, <c>new Project(1, "")</c> succeeded and <c>project.ProjectName = null</c>
/// succeeded, so "project" was a schema with C# syntax rather than a model.
///
/// <para>
/// The collection is <see cref="IReadOnlyList{T}"/> on purpose. A public setter, or a
/// settable <c>List</c>, lets any caller replace or mutate the projects attached to an
/// experience while the aggregate believes nothing changed.
/// </para>
/// </remarks>
public class Project
{
    public int Id { get; }

    public int? ExperienceId { get; private set; }

    public string ProjectName { get; private set; }

    public string? Description { get; private set; }

    public string? Contribution { get; private set; }

    /// <summary>
    /// The stacks this project used, as references rather than entities (plan 3.4).
    /// </summary>
    /// <remarks>
    /// <see cref="TechStack"/> is shared reference data with no per-project state, so
    /// holding it here would let a project change a row every other project cites. A
    /// <see cref="TechStackRef"/> is an immutable snapshot identified by id.
    /// </remarks>
    public IReadOnlyList<TechStackRef> TechStacks { get; private set; } = Array.Empty<TechStackRef>();

    public Project(int id, string projectName)
        : this(id, null, projectName, null, null)
    {
    }

    public Project(
        int id,
        int? experienceId,
        string projectName,
        string? description,
        string? contribution,
        IEnumerable<TechStackRef>? techStacks = null)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty.", nameof(projectName));

        Id = id;
        ExperienceId = experienceId;
        ProjectName = projectName;
        Description = description;
        Contribution = contribution;
        ReplaceTechStacks(techStacks);
    }

    public void AttachToExperience(int? experienceId) => ExperienceId = experienceId;

    public void UpdateDescription(string? description) => Description = description;

    public void UpdateContribution(string? contribution) => Contribution = contribution;

    /// <summary>
    /// Replaces the tech stacks. The argument is copied, so a caller that keeps its own
    /// list cannot change this project afterwards.
    /// </summary>
    public void ReplaceTechStacks(IEnumerable<TechStackRef>? techStacks) =>
        TechStacks = techStacks is null ? Array.Empty<TechStackRef>() : techStacks.ToList();
}
