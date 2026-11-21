using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.ValueObjects;

public class CompanyTests
{
    [Fact]
    public void Constructor_WithName_KeepsTheName()
    {
        var company = new Company("Acme");

        Assert.Equal("Acme", company.Name);
    }

    /// <summary>
    /// Plan 3.1: the empty-string representation is gone. "Not disclosed" is the
    /// absence of a <see cref="Company"/> (<c>Company?</c> in the aggregate), not a
    /// company whose name happens to be blank — which is a value that cannot exist.
    /// The mapping applies the same rule it applies to <c>Role</c>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNoName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Company(name!));
    }

    [Fact]
    public void Constructor_WithNameLongerThan255Characters_Throws()
    {
        var tooLong = new string('a', 256);

        Assert.Throws<ArgumentException>(() => new Company(tooLong));
    }

    [Fact]
    public void ToString_ReturnsTheName()
    {
        var company = new Company("Acme");

        Assert.Equal("Acme", company.ToString());
    }

    [Fact]
    public void TwoCompaniesWithTheSameName_AreEqual()
    {
        Assert.Equal(new Company("Acme"), new Company("Acme"));
        Assert.True(new Company("Acme") == new Company("Acme"));
        Assert.NotEqual(new Company("Acme"), new Company("Other"));
    }

    [Fact]
    public void TwoCompaniesWithTheSameName_ShareAHashCode()
    {
        Assert.Equal(new Company("Acme").GetHashCode(), new Company("Acme").GetHashCode());
    }
}
