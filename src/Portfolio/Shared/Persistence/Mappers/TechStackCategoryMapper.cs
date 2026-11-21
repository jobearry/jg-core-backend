using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Mappers;

public static class TechStackCategoryMapper
{
    public static TechStackCategory ToDomain(TechStackCategoryEntity source) =>
        new(source.Id, source.Category);

    public static TechStackCategoryEntity ToEntity(TechStackCategory source) =>
        new()
        {
            Id = source.Id,
            Category = source.Category
        };
}
