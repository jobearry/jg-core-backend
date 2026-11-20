using System;

namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// The role held at a company.
/// </summary>
/// <remarks>
/// A <c>record</c>, not a class: a value object is defined by its value, so
/// <c>new Role("Developer") == new Role("Developer")</c> has to hold. As a class with no
/// <c>Equals</c> override that comparison is reference-based — which makes a role
/// useless as a dictionary key and impossible to compare against one mapped from the
/// database.
///
/// <para>
/// The constructor is the invariant: a role is never blank. "Not disclosed" is the
/// <i>absence</i> of a role (<c>Role?</c> is null at the aggregate), not a role whose
/// value happens to be empty.
/// </para>
/// </remarks>
public sealed record Role
{
    public const int MaxLength = 50;

    public string Value { get; }

    public Role(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Role value cannot be empty.", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"Role value cannot exceed {MaxLength} characters.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}
