using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Tests;

public class ExperienceMapperTests : MappingTestBase
{
    private static readonly DateTime StartedAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FinishedAt = new(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Map_ExperienceEntity_To_Experience_MapsCompanyAndRole()
    {
        var entity = new ExperienceEntity
        {
            Id = 3,
            CompanyName = "Acme Corp",
            StartedAt = StartedAt,
            Role = "Senior Developer"
        };

        var experience = Mapper.Map<Experience>(entity);

        Assert.Equal(3, experience.Id);
        Assert.Equal("Acme Corp", experience.Company?.Name);
        Assert.Equal("Senior Developer", experience.Role?.Value);
        Assert.Equal(StartedAt, experience.Duration.StartedAt);
    }

    /// <summary>
    /// The domain entity declares <c>Description</c>, <c>Responsibility</c> and
    /// <c>Type</c> with private setters. The hand-written mapper populates them through
    /// the entity's own methods; this asserts they are not silently left null.
    /// </summary>
    [Fact]
    public void Map_ExperienceEntity_To_Experience_PopulatesDescriptionResponsibilityAndType()
    {
        var entity = new ExperienceEntity
        {
            Id = 3,
            CompanyName = "Acme Corp",
            StartedAt = StartedAt,
            Role = "Senior Developer",
            Description = "Built things",
            Responsibility = "Owned the platform",
            Type = "Full time"
        };

        var experience = Mapper.Map<Experience>(entity);

        Assert.Equal("Built things", experience.Description);
        Assert.Equal("Owned the platform", experience.Responsibility);
        Assert.Equal("Full time", experience.Type);
    }

    [Fact]
    public void Map_ExperienceEntity_To_Experience_WithNoEndDate_LeavesTheRoleOngoing()
    {
        var entity = new ExperienceEntity
        {
            Id = 3,
            CompanyName = "Acme Corp",
            StartedAt = StartedAt,
            Role = "Senior Developer"
        };

        var experience = Mapper.Map<Experience>(entity);

        Assert.Null(experience.Duration.FinishedAt);
    }

    /// <summary>
    /// Regression guard for plan 001 item 1.3. The mapping used to hardcode
    /// <c>new Period(src.StartedAt, null)</c>, so <c>FinishedAt</c> was discarded
    /// and the duration of a past role grew every day.
    /// </summary>
    [Fact]
    public void Map_ExperienceEntity_To_Experience_CarriesFinishedAt()
    {
        var entity = new ExperienceEntity
        {
            Id = 3,
            CompanyName = "Acme Corp",
            StartedAt = StartedAt,
            FinishedAt = FinishedAt,
            Role = "Senior Developer"
        };

        var experience = Mapper.Map<Experience>(entity);

        Assert.Equal(FinishedAt, experience.Duration.FinishedAt);
    }

    /// <summary>
    /// The point of carrying <c>FinishedAt</c>: the duration of a role that has
    /// ended is measured against its end date, not against now.
    /// </summary>
    [Fact]
    public void Map_ExperienceEntity_To_Experience_WithFinishedAt_MeasuresDurationToTheEndDate()
    {
        var entity = new ExperienceEntity
        {
            Id = 3,
            CompanyName = "Acme Corp",
            StartedAt = StartedAt,
            FinishedAt = FinishedAt,
            Role = "Senior Developer"
        };

        var experience = Mapper.Map<Experience>(entity);

        // Note the double "Duration": Experience.Duration is the Period value object,
        // and Period.Duration is the TimeSpan it computes. The DTO layer flattens
        // this in plan 001 item 2.1.
        Assert.Equal(FinishedAt - StartedAt, experience.Duration.Duration!.Value);
    }
}
