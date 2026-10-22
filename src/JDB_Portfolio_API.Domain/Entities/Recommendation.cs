using System;
using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Entities;

/// <summary>
/// A testimonial: who wrote it, what they said, and when.
/// </summary>
/// <remarks>
/// Plan 3.3 gave <see cref="Description"/> and <see cref="PersonName"/> the validation
/// that only the latter half-had: <c>PersonName</c> rejected <c>null</c> but accepted
/// <c>""</c> and whitespace (KI-041), and <c>Description</c> was not checked at all, so a
/// nameless, wordless testimonial could be constructed and would render as an empty card.
///
/// <para>
/// <see cref="CareerPosition"/> stays optional: some recommenders do not want their title
/// published, and the mapping turns a blank column into <c>null</c>.
/// </para>
/// </remarks>
public class Recommendation
{
    public int Id { get; }

    public string PersonName { get; private set; }

    public string Description { get; private set; }

    public Role? CareerPosition { get; private set; }

    public DateTime PostedAt { get; }

    public Recommendation(
        int id,
        string personName,
        string description,
        Role? careerPosition,
        DateTime postedAt)
    {
        if (string.IsNullOrWhiteSpace(personName))
            throw new ArgumentException("Person name cannot be empty.", nameof(personName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Recommendation description cannot be empty.", nameof(description));

        Id = id;
        PersonName = personName;
        Description = description;
        CareerPosition = careerPosition;
        PostedAt = postedAt;
    }

    public void ChangeCareerPosition(Role? careerPosition) => CareerPosition = careerPosition;
}
