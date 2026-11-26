using Portfolio.Application.DTO;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Experience;
using Portfolio.Features.Project;
using Portfolio.Features.Skill;
using Portfolio.Features.Recommendation;
using Portfolio.Features.Notion;

namespace Portfolio.Application.Tests;

/// <summary>
/// Plan 001 item 2.1 — the DTO boundary (ADR-0004).
///
/// Two kinds of test live here:
///
/// <list type="number">
///   <item><b>Wire-shape guards.</b> Assertions on the DTO types themselves, so that
///   a field the API should not publish cannot come back by accident. These are the
///   tests that would have caught <c>createdAt</c> or the <c>TimeSpan</c> string
///   leaking onto the wire.</item>
///   <item><b>Mapping tests.</b> Domain in, DTO out, field by field.</item>
/// </list>
///
/// The mapping is hand-written on purpose (ADR-0004): AutoMapper by convention is
/// what produced the current accidental contract, and a second convention-mapped
/// layer would reproduce the same class of bug one level out.
/// </summary>
public class PortfolioDtoMapperTests
{
    // ---------------------------------------------------------------- wire shape

    [Fact]
    public void ExperienceDto_ReplacesTheDurationTimeSpanWithMonths()
    {
        // The API used to serialize Period.Duration as "1231.05:06:07" — a TimeSpan
        // string no client asked for and no client can render.
        Assert.Null(typeof(ExperienceDto).GetProperty("Duration"));

        var months = typeof(ExperienceDto).GetProperty("DurationMonths");
        Assert.NotNull(months);
        Assert.Equal(typeof(int?), months.PropertyType);
    }

    [Fact]
    public void ExperienceDto_FlattensCompanyAndRoleToStrings()
    {
        Assert.Equal(typeof(string), typeof(ExperienceDto).GetProperty("Company")!.PropertyType);
        Assert.Equal(typeof(string), typeof(ExperienceDto).GetProperty("Role")!.PropertyType);
    }

    [Fact]
    public void ProjectDto_DoesNotExposeExperienceId()
    {
        // A project is its own aggregate (ADR-0003 D3); its internal link back to the
        // role is not part of the published contract.
        Assert.Null(typeof(ProjectDto).GetProperty("ExperienceId"));
    }

    [Fact]
    public void TechStackDto_DoesNotExposeCreatedAt()
    {
        Assert.Null(typeof(TechStackDto).GetProperty("CreatedAt"));
    }

    [Fact]
    public void RecommendationDto_FlattensCareerPositionToString()
    {
        Assert.Equal(typeof(string), typeof(RecommendationDto).GetProperty("CareerPosition")!.PropertyType);
    }

    // ------------------------------------------------------------------- tech stack

    [Fact]
    public void MapsTechStack()
    {
        var techStack = new TechStack(2, "C#", "https://example.test/csharp.svg", "Backend");

        var dto = techStack.ToDto();

        Assert.Equal(2, dto.Id);
        Assert.Equal("C#", dto.StackName);
        Assert.Equal("https://example.test/csharp.svg", dto.ImgSrc);
        Assert.Equal("Backend", dto.Category);
    }

    // --------------------------------------------------------------------- project

    [Fact]
    public void MapsProjectWithItsTechStacks()
    {
        var project = new Project(
            7,
            null,
            "Portfolio API",
            "The API behind this site",
            "Everything",
            new[] { new TechStackRef(2, "C#", null, "Backend") });

        var dto = project.ToDto();

        Assert.Equal(7, dto.Id);
        Assert.Equal("Portfolio API", dto.ProjectName);
        Assert.Equal("The API behind this site", dto.Description);
        Assert.Equal("Everything", dto.Contribution);
        var techStack = Assert.Single(dto.TechStacks);
        Assert.Equal("C#", techStack.StackName);
    }

    [Fact]
    public void MapsProjectWithNoTechStacksToAnEmptyList()
    {
        var project = new Project(7, "Portfolio API");

        var dto = project.ToDto();

        Assert.Empty(dto.TechStacks);
    }

    // ------------------------------------------------------------------ experience

    [Fact]
    public void MapsExperienceWithItsProjects()
    {
        var experience = new Experience(
            4,
            new Company("Applied ICT Inc."),
            new Period(new DateTime(2026, 6, 25), null),
            new Role("Software Engineer"));

        experience.UpdateDescription("Transitioned into Software Engineer role");

        // Plan 3.3 gave these two their own mutators. Before, they had only a private
        // setter, so a test had to reach for reflection — a symptom of the missing
        // behaviour, not a testing preference.
        experience.UpdateResponsibility("Maintain enterprise insurance software");
        experience.UpdateType("Corporate");

        experience.ReplaceProjects(new[] { new Project(7, "Portfolio API") });

        var dto = experience.ToDto(new DateTime(2026, 10, 22));

        Assert.Equal(4, dto.Id);
        Assert.Equal("Applied ICT Inc.", dto.Company);
        Assert.Equal("Software Engineer", dto.Role);
        Assert.Equal("Transitioned into Software Engineer role", dto.Description);
        Assert.Equal("Maintain enterprise insurance software", dto.Responsibility);
        Assert.Equal("Corporate", dto.Type);
        var project = Assert.Single(dto.Projects);
        Assert.Equal("Portfolio API", project.ProjectName);
    }

    [Fact]
    public void MapsExperienceWithNoProjectsToAnEmptyList()
    {
        var experience = new Experience(
            4,
            new Company("Acme"),
            new Period(new DateTime(2026, 6, 25), null),
            new Role("Engineer"));

        var dto = experience.ToDto(new DateTime(2026, 10, 22));

        Assert.Empty(dto.Projects);
    }

    /// <summary>
    /// An undeclared role end-to-end: blank in the database, <c>null</c> in the domain
    /// (plan 1.5), and therefore <c>null</c> on the wire. It must not become an empty
    /// string or a missing key.
    /// </summary>
    [Fact]
    public void MapsExperienceWithAnUndeclaredRoleToNull()
    {
        var experience = new Experience(
            4,
            new Company("Acme"),
            new Period(new DateTime(2020, 1, 1), null),
            null);

        var dto = experience.ToDto(new DateTime(2026, 10, 22));

        Assert.Null(dto.Role);
    }

    // --------------------------------------------------------------- duration months

    [Fact]
    public void DurationMonths_ForAFinishedRole_CountsToTheEndDate()
    {
        var experience = new Experience(
            3,
            new Company("SHI"),
            new Period(new DateTime(2023, 5, 2), new DateTime(2026, 6, 24)),
            new Role("Full-stack Web Developer"));

        var dto = experience.ToDto(new DateTime(2026, 10, 22));

        // 2023-05-02 -> 2026-06-24 is 37 whole months and 22 days.
        Assert.Equal(37, dto.DurationMonths);
        Assert.Equal(new DateTime(2026, 6, 24), dto.FinishedAt);
    }

    [Fact]
    public void DurationMonths_ForAnOngoingRole_CountsToNow()
    {
        var experience = new Experience(
            4,
            new Company("Acme"),
            new Period(new DateTime(2020, 1, 1), null),
            new Role("Engineer"));

        var dto = experience.ToDto(new DateTime(2021, 1, 1));

        Assert.Equal(12, dto.DurationMonths);
        Assert.Null(dto.FinishedAt);
    }

    [Fact]
    public void DurationMonths_WhenLessThanAFullMonth_HasPassed_IsZero()
    {
        var experience = new Experience(
            1,
            new Company("Acme"),
            new Period(new DateTime(2020, 1, 31), new DateTime(2020, 2, 28)),
            new Role("Intern"));

        var dto = experience.ToDto(new DateTime(2020, 3, 1));

        Assert.Equal(0, dto.DurationMonths);
    }

    [Fact]
    public void DurationMonths_WhenTheStartDateIsUnknown_IsNull()
    {
        var experience = new Experience(
            1,
            new Company("Acme"),
            new Period(null, null),
            new Role("Intern"));

        var dto = experience.ToDto(new DateTime(2026, 10, 22));

        Assert.Null(dto.DurationMonths);
        Assert.Null(dto.StartedAt);
    }

    // --------------------------------------------------------------- recommendation

    [Fact]
    public void MapsRecommendation()
    {
        var recommendation = new Recommendation(
            1,
            "Atsushi Deguchi",
            "I was able to enjoy working with Jonathan",
            new Role("Engineer"),
            new DateTime(2026, 6, 27, 12, 26, 0));

        var dto = recommendation.ToDto();

        Assert.Equal(1, dto.Id);
        Assert.Equal("Atsushi Deguchi", dto.PersonName);
        Assert.Equal("I was able to enjoy working with Jonathan", dto.Description);
        Assert.Equal("Engineer", dto.CareerPosition);
        Assert.Equal(new DateTime(2026, 6, 27, 12, 26, 0), dto.PostedAt);
    }

    [Fact]
    public void MapsRecommendationWithAnUndeclaredCareerPositionToNull()
    {
        var recommendation = new Recommendation(
            2,
            "Kentaro Komiya",
            "Grateful for the hard work",
            null,
            new DateTime(2026, 6, 27, 12, 27, 0));

        var dto = recommendation.ToDto();

        Assert.Null(dto.CareerPosition);
    }

    // -------------------------------------------------------------------- read models

    [Fact]
    public void MapsPageContent()
    {
        var page = new PageContent("https://notion.so/page", "2026-06-27", "About me");

        var dto = page.ToDto();

        Assert.Equal("https://notion.so/page", dto.Url);
        Assert.Equal("2026-06-27", dto.Date);
        Assert.Equal("About me", dto.Title);
    }

}
