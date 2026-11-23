using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

public class TechStackEntityMappingTests
{
    /// <summary>
    /// Regression guard for plan 001 item 1.2: the domain's <c>Category</c> is a name,
    /// while the entity's <c>TechStackCategoryId</c> is an int. The id must come from
    /// the caller's int argument, never from the name string.
    /// </summary>
    [Fact]
    public void Map_TechStack_To_TechStackEntity_UsesTheGivenCategoryId()
    {
        var stack = new TechStack(1, "Angular", "angular.png", "Frontend");

        var entity = TechStackMapper.ToEntity(stack, 42);

        Assert.Equal(42, entity.TechStackCategoryId);
        Assert.Equal("Angular", entity.StackName);
        Assert.Equal("angular.png", entity.ImgSrc);
    }

    [Fact]
    public void Map_TechStackCategory_To_TechStackCategoryEntity_PreservesTheName()
    {
        var category = new TechStackCategory(2, "Frontend");

        var entity = TechStackCategoryMapper.ToEntity(category);

        Assert.Equal(2, entity.Id);
        Assert.Equal("Frontend", entity.Category);
    }

    [Fact]
    public void Map_TechStackCategoryEntity_To_TechStackCategory_PreservesTheName()
    {
        var entity = new TechStackCategoryEntity { Id = 2, Category = "Frontend" };

        var category = TechStackCategoryMapper.ToDomain(entity);

        Assert.Equal(2, category.Id);
        Assert.Equal("Frontend", category.Category);
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
