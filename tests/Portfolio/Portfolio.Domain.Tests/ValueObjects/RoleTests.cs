using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.ValueObjects;

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

    /// <summary>
    /// The point of a value object: two roles with the same text are the same role.
    /// A class without an <c>Equals</c> override answers <c>false</c> here.
    /// </summary>
    [Fact]
    public void TwoRolesWithTheSameValue_AreEqual()
    {
        Assert.Equal(new Role("Developer"), new Role("Developer"));
        Assert.True(new Role("Developer") == new Role("Developer"));
        Assert.NotEqual(new Role("Developer"), new Role("Engineer"));
    }

    [Fact]
    public void TwoRolesWithTheSameValue_ShareAHashCode()
    {
        Assert.Equal(new Role("Developer").GetHashCode(), new Role("Developer").GetHashCode());
    }

    /// <summary>
    /// The practical consequence of equality: a role can be a dictionary key, which
    /// reference equality makes impossible (every lookup misses).
    /// </summary>
    [Fact]
    public void A_Role_CanBeUsedAsADictionaryKey()
    {
        var roles = new Dictionary<Role, string> { [new Role("Developer")] = "dev" };

        Assert.Equal("dev", roles[new Role("Developer")]);
    }
}
