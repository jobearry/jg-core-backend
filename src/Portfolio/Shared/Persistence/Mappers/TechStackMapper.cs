using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Mappers;

public static class TechStackMapper
{
    public static TechStack ToDomain(TechStackEntity source) =>
        new(
            source.Id,
            source.StackName,
            source.ImgSrc,
            MappingHelpers.ToCategoryName(source.TechStackCategory));

    public static TechStackEntity ToEntity(TechStack source) =>
        new()
        {
            Id = source.Id,
            StackName = source.StackName,
            ImgSrc = source.ImgSrc
        };

    internal static TechStackRef ToRef(ProjectTechStack source) =>
        new(
            source.TechStack.Id,
            source.TechStack.StackName,
            source.TechStack.ImgSrc,
            MappingHelpers.ToCategoryName(source.TechStack.TechStackCategory));
}
