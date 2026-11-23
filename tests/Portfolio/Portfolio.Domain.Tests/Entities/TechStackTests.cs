using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Tests.Entities;

/// <summary>
/// Plan 3.3 — <see cref="TechStack"/> had no constructor at all, so every value was
/// assigned through public setters and a stack with no name was perfectly valid. The audit
/// timestamp left the domain in the same item: <c>CreatedAt</c> belongs to
/// <c>TechStackEntity</c>, not to a technology.
/// </summary>
public class TechStackTests
{
    [Fact]
    public void Constructor_KeepsEveryValue()
    {
        var stack = new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend");

        Assert.Equal(2, stack.Id);
        Assert.Equal("C#", stack.StackName);
        Assert.Equal("https://example.test/csharp.svg", stack.ImgSrc);
        Assert.Equal("Backend", stack.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankStackName_Throws(string? stackName)
    {
        Assert.Throws<ArgumentException>(() => new TechStack(2, stackName!));
    }

    /// <summary>
    /// Null stays legal: a stack can be read as a nested object whose category
    /// navigation was not loaded. Blank does not, because a category that exists and is
    /// empty is a data defect rather than an absent value.
    /// </summary>
    [Fact]
    public void Constructor_WithNoCategory_IsAllowed()
    {
        var stack = new TechStack(2, "C#");

        Assert.Null(stack.Category);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankCategory_Throws(string category)
    {
        Assert.Throws<ArgumentException>(() => new TechStack(2, "C#", null, category));
    }

    [Fact]
    public void SetCategory_WithBlank_Throws()
    {
        var stack = new TechStack(2, "C#", null, "Backend");

        Assert.Throws<ArgumentException>(() => stack.SetCategory(" "));
        Assert.Equal("Backend", stack.Category);
    }

    [Fact]
    public void UpdateDetails_ChangesNameAndImage_KeepsIdAndCategory()
    {
        var stack = new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend");

        stack.UpdateDetails("C# 12", "https://example.test/csharp12.svg");

        Assert.Equal(2, stack.Id);
        Assert.Equal("C# 12", stack.StackName);
        Assert.Equal("https://example.test/csharp12.svg", stack.ImgSrc);
        Assert.Equal("Backend", stack.Category);
    }

    [Fact]
    public void UpdateDetails_WithNoImage_ClearsTheImage()
    {
        var stack = new TechStack(2, "C#", "csharp.svg", "Backend");

        stack.UpdateDetails("C# 12", null);

        Assert.Null(stack.ImgSrc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithBlankName_ThrowsAndKeepsTheOldValues(string? stackName)
    {
        var stack = new TechStack(2, "C#", "csharp.svg", "Backend");

        Assert.Throws<ArgumentException>(() => stack.UpdateDetails(stackName!, "new.svg"));
        Assert.Equal("C#", stack.StackName);
        Assert.Equal("csharp.svg", stack.ImgSrc);
    }

    [Fact]
    public void DomainType_DoesNotCarryCreatedAt()
    {
        Assert.Null(typeof(TechStack).GetProperty("CreatedAt"));
        Assert.Null(typeof(TechStackCategory).GetProperty("CreatedAt"));
    }

    [Fact]
    public void Category_HasNoPublicSetter()
    {
        Assert.Null(typeof(TechStack).GetProperty(nameof(TechStack.Category))!.GetSetMethod());
    }
}
