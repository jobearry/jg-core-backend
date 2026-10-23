using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Tests.ValueObjects;

/// <summary>
/// Plan 3.4 — <see cref="TechStackRef"/> replaced the <c>TechStack</c> entity inside
/// <see cref="JDB_Portfolio_API.Domain.Entities.Project"/>. It has to be a value object to
/// be worth anything: a reference is identified by its id, and two references to the same
/// stack must compare equal so a collection can be diffed.
/// </summary>
public class TechStackRefTests
{
    [Fact]
    public void EqualValues_AreEqual()
    {
        var left = new TechStackRef(2, "C#", "csharp.svg", "Backend");
        var right = new TechStackRef(2, "C#", "csharp.svg", "Backend");

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void DifferentIds_AreNotEqual()
    {
        Assert.NotEqual(new TechStackRef(2, "C#"), new TechStackRef(3, "C#"));
    }

    [Fact]
    public void CanBeUsedAsADictionaryKey()
    {
        var lookup = new Dictionary<TechStackRef, string>
        {
            [new TechStackRef(2, "C#", null, "Backend")] = "backend"
        };

        Assert.Equal("backend", lookup[new TechStackRef(2, "C#", null, "Backend")]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankStackName_Throws(string? stackName)
    {
        Assert.Throws<ArgumentException>(() => new TechStackRef(2, stackName!));
    }

    [Fact]
    public void Constructor_WithNoCategory_IsAllowed()
    {
        var reference = new TechStackRef(2, "C#");

        Assert.Null(reference.Category);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankCategory_Throws(string category)
    {
        Assert.Throws<ArgumentException>(() => new TechStackRef(2, "C#", null, category));
    }
}
