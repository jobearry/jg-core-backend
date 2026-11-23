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

    /// <summary>
    /// The category id is a separate argument because the domain holds the category
    /// <em>name</em>; mapping a string into the int foreign key is the bug this guards.
    /// </summary>
    public static TechStackEntity ToEntity(TechStack source, int categoryId) =>
        new()
        {
            Id = source.Id,
            StackName = source.StackName,
            ImgSrc = source.ImgSrc,
            TechStackCategoryId = categoryId
        };

    internal static TechStackRef ToRef(ProjectTechStack source) =>
        new(
            source.TechStack.Id,
            source.TechStack.StackName,
            source.TechStack.ImgSrc,
            MappingHelpers.ToCategoryName(source.TechStack.TechStackCategory));
}
