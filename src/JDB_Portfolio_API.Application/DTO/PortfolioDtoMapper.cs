using JDB_Portfolio_API.Application.Models;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Application.DTO;

/// <summary>
/// Explicit domain → DTO mapping. Hand-written on purpose (ADR-0004): the accidental
/// contract this replaces was produced by AutoMapper convention, and a second
/// convention-mapped layer would reproduce the same class of bug one level out.
///
/// <para>
/// These are extension methods, so the call site reads <c>project.ToDto()</c>. Nothing
/// here knows about EF Core or HTTP; the DTOs are plain records.
/// </para>
/// </summary>
public static class PortfolioDtoMapper
{
    public static TechStackDto ToDto(this TechStack techStack) =>
        new(
            techStack.Id,
            techStack.StackName,
            techStack.ImgSrc,
            techStack.Category);

    /// <summary>
    /// A referenced stack publishes the same shape as a standalone one (plan 3.4): the
    /// change was in how the aggregate holds a stack, not in what the client sees.
    /// </summary>
    public static TechStackDto ToDto(this TechStackRef techStack) =>
        new(
            techStack.Id,
            techStack.StackName,
            techStack.ImgSrc,
            techStack.Category);

    public static ProjectDto ToDto(this Project project) =>
        new(
            project.Id,
            project.ProjectName,
            project.Description,
            project.Contribution,
            project.TechStacks
                .Select(techStack => techStack.ToDto())
                .ToList());

    /// <summary>
    /// Maps a role. <paramref name="now"/> exists so that the duration of an ongoing
    /// role is deterministic in tests; production passes nothing and gets the clock.
    /// </summary>
    public static ExperienceDto ToDto(this Experience experience, DateTime? now = null)
    {
        var startedAt = experience.Duration.StartedAt;
        var finishedAt = experience.Duration.FinishedAt;

        return new ExperienceDto(
            experience.Id,
            experience.Company.Name,
            experience.Role?.Value,
            startedAt,
            finishedAt,
            DurationInMonths(startedAt, finishedAt ?? now ?? DateTime.UtcNow),
            experience.Description,
            experience.Responsibility,
            experience.Type,
            (experience.Projects)
                .Select(project => project.ToDto())
                .ToList());
    }

    public static RecommendationDto ToDto(this Recommendation recommendation) =>
        new(
            recommendation.Id,
            recommendation.PersonName,
            recommendation.Description,
            recommendation.CareerPosition?.Value,
            recommendation.PostedAt);

    public static DashboardItemDto ToDto(this DashboardItem dashboardItem) =>
        new(
            dashboardItem.Title,
            dashboardItem.Subtitle,
            dashboardItem.Icon,
            dashboardItem.Count,
            dashboardItem.Description);

    public static NotionPageDto ToDto(this NotionPage notionPage) =>
        new(
            notionPage.Url,
            notionPage.Date,
            notionPage.Title);

    /// <summary>
    /// Whole calendar months between two dates — what a CV actually displays.
    ///
    /// <para>
    /// Calendar arithmetic, not <c>TimeSpan.TotalDays / 30</c>: a 30-day divisor
    /// drifts (12 months becomes 11 or 13 depending on the year) and nobody can
    /// explain the result. A partial month at the end does not count, so
    /// 31 Jan → 28 Feb is zero months and 1 Jan → 31 Jan is also zero.
    /// </para>
    /// </summary>
    private static int? DurationInMonths(DateTime? startedAt, DateTime end)
    {
        if (startedAt is null)
            return null;

        var start = startedAt.Value;
        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;

        if (end.Day < start.Day)
            months--;

        // Period already forbids FinishedAt < StartedAt, so this only guards against
        // a clock that runs backwards. A negative duration is nonsense on the wire.
        return months < 0 ? 0 : months;
    }
}
