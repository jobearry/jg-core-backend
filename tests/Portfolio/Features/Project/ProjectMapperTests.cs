using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// Project is now written as well as read, so both mapper directions are live. The
/// write direction must not touch the many-to-many rows: those belong to their own slice.
/// </summary>
public class ProjectMapperTests
{
    [Fact]
    public void Map_ProjectEntity_To_Project_PreservesFieldsAndStackRefs()
    {
        var entity = new ProjectEntity
        {
            Id = 7,
            ExperienceId = 4,
            ProjectName = "Portfolio API",
            Description = "A backend",
            Contribution = "All of it"
        };
        entity.ProjectTechStacks.Add(new ProjectTechStack
        {
            ProjectId = 7,
            TechStackId = 2,
            TechStack = new TechStackEntity
            {
                Id = 2,
                StackName = "C#",
                TechStackCategory = new TechStackCategoryEntity { Id = 1, Category = "Backend" }
            }
        });

        var project = ProjectMapper.ToDomain(entity);

        Assert.Equal(7, project.Id);
        Assert.Equal(4, project.ExperienceId);
        Assert.Equal("Portfolio API", project.ProjectName);
        Assert.Equal("A backend", project.Description);
        Assert.Equal("All of it", project.Contribution);
        Assert.Equal(new TechStackRef(2, "C#", null, "Backend"), Assert.Single(project.TechStacks));
    }

    [Fact]
    public void Map_Project_To_ProjectEntity_PreservesFieldsAndLeavesJoinRowsAlone()
    {
        var project = new Project(
            7,
            4,
            "Portfolio API",
            "A backend",
            "All of it",
            new[] { new TechStackRef(2, "C#", null, "Backend") });

        var entity = ProjectMapper.ToEntity(project);

        Assert.Equal(7, entity.Id);
        Assert.Equal(4, entity.ExperienceId);
        Assert.Equal("Portfolio API", entity.ProjectName);
        Assert.Equal("A backend", entity.Description);
        Assert.Equal("All of it", entity.Contribution);
        Assert.Empty(entity.ProjectTechStacks);
    }
}
