using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.Entities;

/// <summary>
/// Plan 3.3 — <see cref="Experience.Projects"/> had a public setter, so any caller could
/// replace the collection while the aggregate believed nothing had changed; and
/// <c>Responsibility</c>/<c>Type</c> had no mutator at all, so the entity could not express
/// two of its own fields (AutoMapper wrote them by reflection instead).
/// </summary>
public class ExperienceTests
{
    private static Experience CreateExperience() =>
        new(4, new Company("Acme"), new Period(new DateTime(2020, 1, 1), null), new Role("Engineer"));

    [Fact]
    public void ReplaceProjects_CopiesTheList()
    {
        var experience = CreateExperience();
        var source = new List<Project> { new(7, "Portfolio API") };

        experience.ReplaceProjects(source);
        source.Add(new Project(8, "Second project"));

        Assert.Single(experience.Projects);
    }

    [Fact]
    public void ReplaceProjects_WithNull_LeavesAnEmptyList()
    {
        var experience = CreateExperience();

        experience.ReplaceProjects(null);

        Assert.Empty(experience.Projects);
    }

    [Fact]
    public void Projects_HasNoPublicSetter()
    {
        Assert.Null(typeof(Experience).GetProperty(nameof(Experience.Projects))!.GetSetMethod());
    }

    [Fact]
    public void UpdateResponsibility_SetsTheValue()
    {
        var experience = CreateExperience();

        experience.UpdateResponsibility("Maintain enterprise insurance software");

        Assert.Equal("Maintain enterprise insurance software", experience.Responsibility);
    }

    [Fact]
    public void UpdateType_SetsTheValue()
    {
        var experience = CreateExperience();

        experience.UpdateType("Corporate");

        Assert.Equal("Corporate", experience.Type);
    }

    [Fact]
    public void UpdateDescription_WithNull_ClearsIt()
    {
        var experience = CreateExperience();
        experience.UpdateDescription("Something");

        experience.UpdateDescription(null);

        Assert.Null(experience.Description);
    }

    [Fact]
    public void UpdateDetails_ChangesAllScalarFields_KeepsIdAndProjects()
    {
        var experience = CreateExperience();
        experience.ReplaceProjects([new Project(7, "Portfolio API")]);

        experience.UpdateDetails(
            "Globex",
            new DateTime(2019, 2, 1),
            new DateTime(2022, 3, 1),
            "Staff Engineer",
            "New description",
            "New responsibility",
            "Contract");

        Assert.Equal(4, experience.Id);
        Assert.Equal("Globex", experience.Company.Name);
        Assert.Equal(new DateTime(2019, 2, 1), experience.Duration.StartedAt);
        Assert.Equal(new DateTime(2022, 3, 1), experience.Duration.FinishedAt);
        Assert.Equal("Staff Engineer", experience.Role?.Value);
        Assert.Equal("New description", experience.Description);
        Assert.Equal("New responsibility", experience.Responsibility);
        Assert.Equal("Contract", experience.Type);
        Assert.Equal("Portfolio API", Assert.Single(experience.Projects).ProjectName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithBlankCompany_ThrowsAndKeepsOldValues(string? company)
    {
        var experience = CreateExperience();

        Assert.Throws<ArgumentException>(() => experience.UpdateDetails(
            company!, null, null, null, null, null, null));

        Assert.Equal("Acme", experience.Company.Name);
        Assert.Equal(new DateTime(2020, 1, 1), experience.Duration.StartedAt);
        Assert.Equal("Engineer", experience.Role?.Value);
    }

    [Fact]
    public void UpdateDetails_WithTooLongCompany_ThrowsAndKeepsOldValues()
    {
        var experience = CreateExperience();
        var company = new string('X', Company.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => experience.UpdateDetails(
            company, null, null, null, null, null, null));

        Assert.Equal("Acme", experience.Company.Name);
    }

    [Fact]
    public void UpdateDetails_WithEndBeforeStart_ThrowsAndKeepsOldValues()
    {
        var experience = CreateExperience();

        Assert.Throws<ArgumentException>(() => experience.UpdateDetails(
            "Globex", new DateTime(2021, 1, 1), new DateTime(2020, 1, 1),
            null, null, null, null));

        Assert.Equal("Acme", experience.Company.Name);
        Assert.Equal(new DateTime(2020, 1, 1), experience.Duration.StartedAt);
        Assert.Null(experience.Duration.FinishedAt);
    }

    [Fact]
    public void UpdateDetails_WithFutureStart_ThrowsAndKeepsOldValues()
    {
        var experience = CreateExperience();
        var future = DateTime.UtcNow.Date.AddDays(2);

        Assert.Throws<ArgumentException>(() => experience.UpdateDetails(
            "Globex", future, null, null, null, null, null));

        Assert.Equal("Acme", experience.Company.Name);
        Assert.Equal(new DateTime(2020, 1, 1), experience.Duration.StartedAt);
    }

    [Fact]
    public void UpdateDetails_WithBlankRole_ClearsTheRole()
    {
        var experience = CreateExperience();

        experience.UpdateDetails(
            "Acme", new DateTime(2020, 1, 1), null, "  ", null, null, null);

        Assert.Null(experience.Role);
    }

    [Fact]
    public void UpdateDetails_WithTooLongRole_ThrowsAndKeepsOldValues()
    {
        var experience = CreateExperience();
        var role = new string('R', Role.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => experience.UpdateDetails(
            "Globex", null, null, role, null, null, null));

        Assert.Equal("Acme", experience.Company.Name);
        Assert.Equal("Engineer", experience.Role?.Value);
    }
}
