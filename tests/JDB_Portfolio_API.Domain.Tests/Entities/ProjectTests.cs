using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Tests.Entities;

/// <summary>
/// Plan 3.3 — <see cref="Project"/> was the clearest case of "an entity that can hold
/// invalid state": <c>new Project(1, "")</c> constructed, and every property accepted
/// <c>null</c> afterwards. These tests pin the constructor, the mutators and the read-only
/// collection.
/// </summary>
public class ProjectTests
{
    [Fact]
    public void Constructor_WithProjectName_KeepsIt()
    {
        var project = new Project(7, "Portfolio API");

        Assert.Equal(7, project.Id);
        Assert.Equal("Portfolio API", project.ProjectName);
        Assert.Null(project.ExperienceId);
        Assert.Empty(project.TechStacks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankProjectName_Throws(string? projectName)
    {
        Assert.Throws<ArgumentException>(() => new Project(7, projectName!));
    }

    [Fact]
    public void Constructor_WithTechStacks_CopiesThem()
    {
        var source = new List<TechStackRef> { new(2, "C#", null, "Backend") };

        var project = new Project(7, null, "Portfolio API", null, null, source);
        source.Add(new TechStackRef(3, "Angular", null, "Frontend"));

        // Copying is the point: a caller that keeps its list must not be able to change
        // what the project contains after construction.
        Assert.Single(project.TechStacks);
    }

    [Fact]
    public void ReplaceTechStacks_WithNull_LeavesAnEmptyList()
    {
        var project = new Project(7, "Portfolio API");

        project.ReplaceTechStacks(null);

        Assert.Empty(project.TechStacks);
    }

    [Fact]
    public void UpdateDescription_WithNull_ClearsIt()
    {
        var project = new Project(7, null, "Portfolio API", "old", null);

        project.UpdateDescription(null);

        Assert.Null(project.Description);
    }

    [Fact]
    public void AttachToExperience_SetsTheExperienceId()
    {
        var project = new Project(7, "Portfolio API");

        project.AttachToExperience(4);

        Assert.Equal(4, project.ExperienceId);
    }

    /// <summary>
    /// The identity mismatch 3.3 asked to settle: <c>Experience.Id</c> was
    /// constructor-only while <c>Project.Id</c> was publicly settable. Both are now
    /// get-only, which is what makes an entity's identity trustworthy.
    /// </summary>
    [Fact]
    public void Id_HasNoPublicSetter()
    {
        Assert.Null(typeof(Project).GetProperty(nameof(Project.Id))!.GetSetMethod());
    }

    /// <summary>
    /// <c>private set</c>, deliberately checked through <c>GetSetMethod()</c> rather than
    /// <c>SetMethod</c>: the latter reports non-public accessors too, which is exactly why
    /// AutoMapper can still write them. The collection is not publicly replaceable.
    /// </summary>
    [Fact]
    public void TechStacks_HasNoPublicSetter()
    {
        Assert.Null(typeof(Project).GetProperty(nameof(Project.TechStacks))!.GetSetMethod());
    }
}
