using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Tests.Entities;

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

    [Fact]
    public void Rename_ChangesTheName_KeepsTheId()
    {
        var category = new TechStackCategory(2, "Frontend");

        category.Rename("Back-end");

        Assert.Equal(2, category.Id);
        Assert.Equal("Back-end", category.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithBlankName_ThrowsAndKeepsTheOldName(string? category)
    {
        var sut = new TechStackCategory(2, "Frontend");

        Assert.Throws<ArgumentException>(() => sut.Rename(category!));
        Assert.Equal("Frontend", sut.Category);
    }
}
