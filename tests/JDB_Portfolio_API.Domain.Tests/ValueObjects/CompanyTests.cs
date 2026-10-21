using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Tests.ValueObjects;

public class CompanyTests
{
    [Fact]
    public void Constructor_WithName_KeepsTheName()
    {
        var company = new Company("Acme");

        Assert.Equal("Acme", company.Name);
    }

    [Fact]
    public void Constructor_WithNullName_MapsToEmptyString()
    {
        var company = new Company(null);

        Assert.Equal("", company.Name);
    }

    [Fact]
    public void Constructor_WithBlankName_MapsToEmptyString()
    {
        var company = new Company("   ");

        Assert.Equal("", company.Name);
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
}
