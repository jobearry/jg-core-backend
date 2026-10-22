using System;

namespace JDB_Portfolio_API.Domain.Entities;

/// <summary>
/// The grouping a <see cref="TechStack"/> belongs to (frontend, backend, …).
/// </summary>
/// <remarks>
/// Plan 3.3: validating constructor, no <c>partial</c>, no <c>= null!</c>, no
/// <c>CreatedAt</c>. The audit column stays in <c>TechStackCategoryEntity</c>.
/// </remarks>
public class TechStackCategory
{
    public int Id { get; }

    public string Category { get; }

    public TechStackCategory(int id, string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category name cannot be empty.", nameof(category));

        Id = id;
        Category = category;
    }
}
