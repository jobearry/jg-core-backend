using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Tests.ValueObjects;

public class RoleTests
{
    [Fact]
    public void Constructor_WithValue_KeepsTheValue()
    {
        var role = new Role("Developer");

        Assert.Equal("Developer", role.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankValue_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new Role(value));
    }

    [Fact]
    public void Constructor_WithNullValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Role(null!));
    }

    [Fact]
    public void Constructor_WithValueLongerThan50Characters_Throws()
    {
        var tooLong = new string('a', 51);

        Assert.Throws<ArgumentException>(() => new Role(tooLong));
    }

    [Fact]
    public void Constructor_WithValueOfExactly50Characters_IsAccepted()
    {
        var atLimit = new string('a', 50);

        var role = new Role(atLimit);

        Assert.Equal(atLimit, role.Value);
    }

    [Fact]
    public void ToString_ReturnsTheValue()
    {
        var role = new Role("Developer");

        Assert.Equal("Developer", role.ToString());
    }
}
