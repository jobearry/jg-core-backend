using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Tests;

public class MappingConfigurationTests : MappingTestBase
{
    /// <summary>
    /// Smoke gate for the hand-written mappers: every persistence entity the API
    /// reads has an explicit Entity → Domain mapping. Runtime mapping tests live
    /// beside each entity; this one catches a mapper that was never wired in.
    /// </summary>
    [Fact]
    public void MappingConfiguration_CoversEveryPersistenceEntity()
    {
        var experience = Mapper.Map<Domain.Entities.Experience>(new ExperienceEntity
        {
            Id = 1,
            CompanyName = "Acme Corp",
            StartedAt = new DateTime(2020, 1, 1),
            Role = "Developer"
        });
        var project = Mapper.Map<Domain.Entities.Project>(new ProjectEntity
        {
            Id = 2,
            ProjectName = "Portfolio API"
        });
        var techStack = Mapper.Map<Domain.Entities.TechStack>(new TechStackEntity
        {
            Id = 3,
            StackName = "C#"
        });
        var category = Mapper.Map<Domain.Entities.TechStackCategory>(new TechStackCategoryEntity
        {
            Id = 4,
            Category = "Language"
        });
        var recommendation = Mapper.Map<Domain.Entities.Recommendation>(new RecommendationEntity
        {
            Id = 5,
            PersonName = "Atsushi Deguchi",
            Description = "Great to work with",
            CareerPosition = "Engineer",
            PostedAt = new DateTime(2026, 6, 27)
        });

        Assert.Equal(1, experience.Id);
        Assert.Equal("Portfolio API", project.ProjectName);
        Assert.Equal("C#", techStack.StackName);
        Assert.Equal("Language", category.Category);
        Assert.Equal("Atsushi Deguchi", recommendation.PersonName);
    }
}
