using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Mappers;

public static class ProjectMapper
{
    public static Project ToDomain(ProjectEntity source) =>
        new(
            source.Id,
            source.ExperienceId,
            source.ProjectName,
            source.Description,
            source.Contribution,
            source.ProjectTechStacks?.Select(TechStackMapper.ToRef).ToList());

    public static ProjectEntity ToEntity(Project source) =>
        new()
        {
            Id = source.Id,
            ExperienceId = source.ExperienceId,
            ProjectName = source.ProjectName,
            Description = source.Description,
            Contribution = source.Contribution
        };
}
