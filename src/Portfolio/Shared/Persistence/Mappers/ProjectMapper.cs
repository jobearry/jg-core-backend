using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
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

    /// <summary>
    /// The join write direction. A <see cref="TechStackRef"/> is an immutable snapshot
    /// of shared reference data, so only its id is persisted. Duplicates are dropped
    /// because the composite key cannot hold them.
    /// </summary>
    public static IEnumerable<ProjectTechStack> ToJoinEntities(Project source) =>
        source.TechStacks
            .DistinctBy(stack => stack.Id)
            .Select(stack => new ProjectTechStack
            {
                ProjectId = source.Id,
                TechStackId = stack.Id
            });

    /// <summary>
    /// Creates a single join entity for an incremental add.
    /// </summary>
    public static ProjectTechStack ToJoinEntity(int projectId, TechStackRef stack) =>
        new ProjectTechStack
        {
            ProjectId = projectId,
            TechStackId = stack.Id
        };
}
