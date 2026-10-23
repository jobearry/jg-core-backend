using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;
using JDB_Portfolio_API.Persistence.Entities;

namespace JDB_Portfolio_API.Persistence.Tests;

/// <summary>
/// Plan 3.1a — a role always names a company.
///
/// <para>
/// 3.1 removed the empty-string company from the value object; 3.1a moved the rule into
/// the schema, so <c>company_name</c> is <c>NOT NULL</c> with a
/// <c>CHECK (btrim(company_name) &lt;&gt; '')</c>. That is why there is no
/// "unnamed company" case to test any more: the previous tests in this file asserted
/// that a null or blank column became <c>null</c>, and that state can no longer exist.
/// </para>
///
/// <para>
/// Companies that cannot be named are not a reason to allow blanks — use
/// "Self-employed" or "Contract — NDA", which are honest values rather than absence.
/// </para>
/// </summary>
public class RequiredCompanyMappingTests : MappingTestBase
{
    [Fact]
    public void Map_ExperienceEntity_WithACompany_KeepsTheName()
    {
        var entity = new ExperienceEntity
        {
            Id = 7,
            CompanyName = "Acme Corp",
            StartedAt = new DateTime(2020, 1, 1),
            Role = "Developer"
        };

        var experience = Mapper.Map<Experience>(entity);

        Assert.Equal(new Company("Acme Corp"), experience.Company);
    }

    [Fact]
    public void Map_Experience_WithACompany_WritesItsName()
    {
        var experience = new Experience(
            7,
            new Company("Acme Corp"),
            new Period(new DateTime(2020, 1, 1), null),
            new Role("Developer"));

        var entity = Mapper.Map<ExperienceEntity>(experience);

        Assert.Equal("Acme Corp", entity.CompanyName);
    }

    /// <summary>
    /// If the constraint is ever dropped and a blank somehow appears, the mapping fails
    /// loudly instead of inventing an interpretation. Silently turning it into
    /// "no company" — what 3.1 did — is only correct while the schema permits the state.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Map_ExperienceEntity_WithABlankCompany_FailsLoudly(string companyName)
    {
        var entity = new ExperienceEntity
        {
            Id = 7,
            CompanyName = companyName,
            StartedAt = new DateTime(2020, 1, 1),
            Role = "Developer"
        };

        Assert.ThrowsAny<Exception>(() => Mapper.Map<Experience>(entity));
    }
}
