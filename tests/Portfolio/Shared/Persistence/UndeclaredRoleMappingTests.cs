using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// Plan 001 item 1.5, as resolved on 2026-10-22 — with the opposite answer to the
/// one the plan first proposed.
///
/// The plan suggested forbidding the empty string with a <c>CHECK</c> constraint.
/// That was rejected: a blank role is a legitimate state meaning "I would rather
/// not declare this role", and a constraint would make it impossible to express.
///
/// The rule therefore lives in the mapping, not in the database:
///
/// <list type="bullet">
///   <item>blank or whitespace in the database  → <c>null</c> in the domain</item>
///   <item><c>null</c> in the domain            → <c>""</c> when written back,
///         because the column is still <c>NOT NULL</c></item>
/// </list>
///
/// The important property is that none of this throws. Before the change,
/// <c>new Role("")</c> threw <see cref="ArgumentException"/> inside AutoMapper and
/// took down the entire collection endpoint for one row.
/// </summary>
public class UndeclaredRoleMappingTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    public void Map_ExperienceEntity_WithUndeclaredRole_ProducesNoRole(string role)
    {
        var entity = new ExperienceEntity
        {
            Id = 7,
            CompanyName = "Acme Corp",
            StartedAt = new DateTime(2020, 1, 1),
            Role = role
        };

        var experience = ExperienceMapper.ToDomain(entity);

        Assert.Null(experience.Role);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    public void Map_RecommendationEntity_WithUndeclaredCareerPosition_ProducesNoCareerPosition(
        string careerPosition)
    {
        var entity = new RecommendationEntity
        {
            Id = 1,
            PersonName = "Atsushi Deguchi",
            Description = "Great to work with",
            CareerPosition = careerPosition,
            PostedAt = new DateTime(2026, 6, 27)
        };

        var recommendation = RecommendationMapper.ToDomain(entity);

        Assert.Null(recommendation.CareerPosition);
    }

    /// <summary>
    /// The database column is still <c>NOT NULL</c>, so "undeclared" has to be
    /// written back as the empty string. Writing <c>null</c> instead would fail at
    /// the database on the first write path.
    /// </summary>
    [Fact]
    public void Map_Experience_WithNoRole_WritesTheEmptyString()
    {
        var experience = new Domain.Entities.Experience(
            7,
            new Domain.ValueObjects.Company("Acme Corp"),
            new Domain.ValueObjects.Period(null, null),
            null);

        var entity = ExperienceMapper.ToEntity(experience);

        Assert.Equal(string.Empty, entity.Role);
    }

    [Fact]
    public void Map_Recommendation_WithNoCareerPosition_WritesTheEmptyString()
    {
        var recommendation = new Domain.Entities.Recommendation(
            1,
            "Atsushi Deguchi",
            "Great to work with",
            null,
            new DateTime(2026, 6, 27));

        var entity = RecommendationMapper.ToEntity(recommendation);

        Assert.Equal(string.Empty, entity.CareerPosition);
    }

    /// <summary>
    /// The counterpart guard: turning "undeclared" into absence must not lose a role
    /// that <em>is</em> declared. The reverse map for <c>CareerPosition</c> previously
    /// assigned a <c>Role</c> object to a <c>string</c> property and relied on
    /// AutoMapper to convert it — the same class of latent bug as KI-011.
    /// </summary>
    [Fact]
    public void Map_Recommendation_WithDeclaredCareerPosition_WritesItsValue()
    {
        var recommendation = new Domain.Entities.Recommendation(
            1,
            "Atsushi Deguchi",
            "Great to work with",
            new Domain.ValueObjects.Role("Engineer"),
            new DateTime(2026, 6, 27));

        var entity = RecommendationMapper.ToEntity(recommendation);

        Assert.Equal("Engineer", entity.CareerPosition);
    }
}
