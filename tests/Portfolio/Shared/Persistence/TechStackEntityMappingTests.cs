using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

public class TechStackEntityMappingTests
{
    /// <summary>
    /// Regression guard for plan 001 item 1.2. The reverse mapping assigned the
    /// domain's <c>Category</c> (a string) to the entity's
    /// <c>TechStackCategoryId</c> (an int). It compiled, and it threw the first
    /// time anything tried to write. The category id is resolved by a lookup at
    /// write time instead, so it is not mapped from a name here.
    /// </summary>
    [Fact]
    public void Map_TechStack_To_TechStackEntity_LeavesTheCategoryIdUnset()
    {
        var stack = new TechStack(1, "Angular", "angular.png", "Frontend");

        var entity = TechStackMapper.ToEntity(stack);

        Assert.Equal(0, entity.TechStackCategoryId);
    }

    [Fact]
    public void Map_TechStackEntity_To_TechStack_MapsTheCategoryName()
    {
        var entity = new TechStackEntity
        {
            Id = 1,
            StackName = "Angular",
            ImgSrc = "angular.png",
            TechStackCategoryId = 2,
            TechStackCategory = new TechStackCategoryEntity { Id = 2, Category = "Frontend" }
        };

        var stack = TechStackMapper.ToDomain(entity);

        Assert.Equal("Frontend", stack.Category);
        Assert.Equal("Angular", stack.StackName);
    }
}
