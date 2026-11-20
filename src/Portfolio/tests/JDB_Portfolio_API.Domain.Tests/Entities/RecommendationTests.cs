using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Tests.Entities;

/// <summary>
/// Plan 3.3 — <see cref="Recommendation"/> validated nothing: <c>PersonName</c> rejected
/// <c>null</c> but accepted whitespace (KI-041) and <c>Description</c> was not checked at
/// all, so a testimonial with no author and no words could be constructed.
/// </summary>
public class RecommendationTests
{
    private static Recommendation CreateRecommendation(
        string personName = "Atsushi Deguchi",
        string description = "Great to work with") =>
        new(1, personName, description, new Role("Engineer"), new DateTime(2026, 6, 27));

    [Fact]
    public void Constructor_KeepsEveryValue()
    {
        var postedAt = new DateTime(2026, 6, 27, 12, 26, 0);

        var recommendation = new Recommendation(1, "Atsushi Deguchi", "Great to work with", new Role("Engineer"), postedAt);

        Assert.Equal(1, recommendation.Id);
        Assert.Equal("Atsushi Deguchi", recommendation.PersonName);
        Assert.Equal("Great to work with", recommendation.Description);
        Assert.Equal(new Role("Engineer"), recommendation.CareerPosition);
        Assert.Equal(postedAt, recommendation.PostedAt);
    }

    /// <summary>
    /// KI-041: <c>personName ?? throw</c> let <c>""</c> and <c>"   "</c> through, which
    /// rendered as a nameless testimonial rather than failing.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPersonName_Throws(string? personName)
    {
        Assert.Throws<ArgumentException>(() => CreateRecommendation(personName: personName!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankDescription_Throws(string? description)
    {
        Assert.Throws<ArgumentException>(() => CreateRecommendation(description: description!));
    }

    [Fact]
    public void Constructor_WithNoCareerPosition_IsAllowed()
    {
        var recommendation = new Recommendation(2, "Kentaro Komiya", "Grateful", null, new DateTime(2026, 6, 27));

        Assert.Null(recommendation.CareerPosition);
    }

    [Fact]
    public void ChangeCareerPosition_CanClearIt()
    {
        var recommendation = CreateRecommendation();

        recommendation.ChangeCareerPosition(null);

        Assert.Null(recommendation.CareerPosition);
    }
}
