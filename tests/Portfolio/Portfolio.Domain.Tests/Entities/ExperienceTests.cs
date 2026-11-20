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
}
