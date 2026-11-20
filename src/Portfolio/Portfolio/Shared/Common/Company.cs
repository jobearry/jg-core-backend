using System;

namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// The company a role was held at.
/// </summary>
/// <remarks>
/// A <c>record</c>: two companies with the same name are the same company.
///
/// <para>
/// A blank name is rejected rather than collapsed to <c>""</c>. The old mapping turned
/// <c>null</c> and <c>"   "</c> into an empty-string company, which meant the model had
/// a value that represented "nothing" — so callers had to check <c>Name == ""</c> in
/// addition to checking for <c>null</c>. "Not disclosed" is not a thing for an employer:
/// plan 3.1a made the column <c>NOT NULL</c> with a non-blank <c>CHECK</c>, and
/// <c>Experience.Company</c> is not nullable either. Use a truthful stand-in such as
/// "Self-employed" when the client cannot be named.
/// </para>
/// </remarks>
public sealed record Company
{
    public const int MaxLength = 255;

    public string Name { get; }

    public Company(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty.", nameof(name));

        if (name.Length > MaxLength)
            throw new ArgumentException($"Company name cannot exceed {MaxLength} characters.", nameof(name));

        Name = name;
    }

    public override string ToString() => Name;
}
