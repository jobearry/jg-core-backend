using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Persistence.Entities;

namespace JDB_Portfolio_API.Persistence.Tests;

public class TechStackEntityMappingTests : MappingTestBase
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

        var entity = Mapper.Map<TechStackEntity>(stack);

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

        var stack = Mapper.Map<TechStack>(entity);

        Assert.Equal("Frontend", stack.Category);
        Assert.Equal("Angular", stack.StackName);
    }
}
