using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Mappers;

public static class RecommendationMapper
{
    public static Recommendation ToDomain(RecommendationEntity source) =>
        new(
            source.Id,
            source.PersonName,
            source.Description,
            MappingHelpers.ToRole(source.CareerPosition),
            source.PostedAt);

    public static RecommendationEntity ToEntity(Recommendation source) =>
        new()
        {
            Id = source.Id,
            PersonName = source.PersonName,
            Description = source.Description,
            CareerPosition = MappingHelpers.ToStoredRole(source.CareerPosition),
            PostedAt = source.PostedAt
        };
}
