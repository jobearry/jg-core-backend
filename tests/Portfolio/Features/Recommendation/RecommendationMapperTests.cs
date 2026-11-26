using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// Recommendation is now written as well as read, so both mapper directions are live.
/// These tests pin the value-object conversion and the identity-always insert path.
/// </summary>
public class RecommendationMapperTests
{
    private static readonly DateTime PostedAt =
        new(2026, 6, 27, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Map_RecommendationEntity_To_Recommendation_PreservesAllFields()
    {
        var entity = new RecommendationEntity
        {
            Id = 7,
            PersonName = "Jane Doe",
            Description = "Excellent teammate",
            CareerPosition = "Staff Engineer",
            PostedAt = PostedAt
        };

        var recommendation = RecommendationMapper.ToDomain(entity);

        Assert.Equal(7, recommendation.Id);
        Assert.Equal("Jane Doe", recommendation.PersonName);
        Assert.Equal("Excellent teammate", recommendation.Description);
        Assert.Equal(new Role("Staff Engineer"), recommendation.CareerPosition);
        Assert.Equal(PostedAt, recommendation.PostedAt);
    }

    [Fact]
    public void Map_RecommendationEntity_To_Recommendation_WithBlankCareerPosition_YieldsNull()
    {
        var entity = new RecommendationEntity
        {
            Id = 7,
            PersonName = "Jane Doe",
            Description = "Excellent teammate",
            CareerPosition = string.Empty,
            PostedAt = PostedAt
        };

        var recommendation = RecommendationMapper.ToDomain(entity);

        Assert.Null(recommendation.CareerPosition);
    }

    [Fact]
    public void Map_Recommendation_To_RecommendationEntity_PreservesAllFields()
    {
        var recommendation = new Recommendation(
            7, "Jane Doe", "Excellent teammate", new Role("Staff Engineer"), PostedAt);

        var entity = RecommendationMapper.ToEntity(recommendation);

        Assert.Equal(7, entity.Id);
        Assert.Equal("Jane Doe", entity.PersonName);
        Assert.Equal("Excellent teammate", entity.Description);
        Assert.Equal("Staff Engineer", entity.CareerPosition);
        Assert.Equal(PostedAt, entity.PostedAt);
    }

    [Fact]
    public void Map_Recommendation_To_RecommendationEntity_WithNoCareerPosition_WritesEmptyString()
    {
        var recommendation = new Recommendation(7, "Jane Doe", "Excellent teammate", null, PostedAt);

        var entity = RecommendationMapper.ToEntity(recommendation);

        Assert.Equal(string.Empty, entity.CareerPosition);
    }

    [Fact]
    public void Map_Recommendation_To_RecommendationEntity_UsesUnspecifiedKindForTimestampColumn()
    {
        var recommendation = new Recommendation(
            7, "Jane Doe", "Excellent teammate", null, PostedAt);

        var entity = RecommendationMapper.ToEntity(recommendation);

        Assert.Equal(PostedAt, entity.PostedAt);
        Assert.Equal(DateTimeKind.Unspecified, entity.PostedAt.Kind);
    }

}
