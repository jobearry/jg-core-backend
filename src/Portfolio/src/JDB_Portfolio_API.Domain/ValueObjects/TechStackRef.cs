using System;

namespace JDB_Portfolio_API.Domain.ValueObjects;

/// <summary>
/// A reference to a <see cref="Entities.TechStack"/> from inside another aggregate.
/// </summary>
/// <remarks>
/// Plan 3.4. <see cref="Entities.TechStack"/> is shared reference data: the same row is
/// cited by many projects and also published on its own by <c>/api/v1/skill</c>. Holding
/// the entity inside <see cref="Entities.Project"/> would make one project's mutation of
/// "C#" the mutation of every project's "C#", across aggregates and outside any
/// transaction boundary. A <c>record</c> gives the project an immutable snapshot that is
/// identified by <see cref="Id"/> instead.
///
/// <para>
/// A value object, not the entity: value equality is what a reference needs, and
/// <c>TechStackRef(2, "C#") == TechStackRef(2, "C#")</c> has to hold for the collection to
/// behave. The fields match what the wire already carries for a nested stack, so the
/// change is invisible to clients.
/// </para>
/// </remarks>
public sealed record TechStackRef
{
    public int Id { get; }

    public string StackName { get; }

    public string? ImgSrc { get; }

    public string? Category { get; }

    public TechStackRef(int id, string stackName, string? imgSrc = null, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(stackName))
            throw new ArgumentException("Stack name cannot be empty.", nameof(stackName));

        // Same rule as the entity: a category that exists may not be blank, but a
        // reference read without its category navigation may legitimately have none.
        if (category is not null && string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be blank when it is given.", nameof(category));

        Id = id;
        StackName = stackName;
        ImgSrc = imgSrc;
        Category = category;
    }
}
