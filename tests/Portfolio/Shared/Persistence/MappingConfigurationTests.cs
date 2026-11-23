using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

public class MappingConfigurationTests
{
    /// <summary>
    /// Smoke gate for the two mappers without a dedicated entity test. Every other
    /// mapper has a runtime test beside its entity; this one catches a mapper that was
    /// never wired in at all.
    /// </summary>
    [Fact]
    public void MappingConfiguration_CoversOtherwiseUncoveredMappers()
    {
        var project = ProjectMapper.ToDomain(new ProjectEntity
        {
            Id = 2,
            ProjectName = "Portfolio API"
        });
        var category = TechStackCategoryMapper.ToDomain(new TechStackCategoryEntity
        {
            Id = 4,
            Category = "Language"
        });

        Assert.Equal("Portfolio API", project.ProjectName);
        Assert.Equal("Language", category.Category);
    }
}
