using JDB_Portfolio_API.Domain.Entities;

namespace JDB_Portfolio_API.Domain.Tests.Entities;

/// <summary>
/// Plan 3.3 — validating constructor, no <c>partial</c>, no <c>= null!</c>, no
/// <c>CreatedAt</c>.
/// </summary>
public class TechStackCategoryTests
{
    [Fact]
    public void Constructor_KeepsTheName()
    {
        var category = new TechStackCategory(2, "Frontend");

        Assert.Equal(2, category.Id);
        Assert.Equal("Frontend", category.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string? category)
    {
        Assert.Throws<ArgumentException>(() => new TechStackCategory(2, category!));
    }
}
