using System;

namespace JDB_Portfolio_API.Domain.Entities;

/// <summary>
/// A tool or technology, with the name of its category flattened onto it.
/// </summary>
/// <remarks>
/// Plan 3.3 replaced object-initializer construction with a validating constructor and
/// dropped <c>CreatedAt</c>. That column is audit bookkeeping — it exists in
/// <c>tech_stack</c> and stays in <c>TechStackEntity</c>, but a stack's identity has
/// nothing to do with when the row was inserted, and the API has not published it since
/// plan 2.1.
///
/// <para>
/// <see cref="Category"/> is nullable because a stack can be read as a nested object
/// whose category navigation was not loaded; when it is present it cannot be blank.
/// </para>
/// </remarks>
public class TechStack
{
    public int Id { get; }

    public string StackName { get; }

    public string? ImgSrc { get; }

    public string? Category { get; private set; }

    public TechStack(int id, string stackName, string? imgSrc = null, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(stackName))
            throw new ArgumentException("Stack name cannot be empty.", nameof(stackName));

        Id = id;
        StackName = stackName;
        ImgSrc = imgSrc;
        SetCategory(category);
    }

    public void SetCategory(string? category)
    {
        if (category is not null && string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be blank when it is given.", nameof(category));

        Category = category;
    }
}
