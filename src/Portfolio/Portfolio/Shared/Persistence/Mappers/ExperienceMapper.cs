using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Mappers;

public static class ExperienceMapper
{
    public static Experience ToDomain(ExperienceEntity source)
    {
        var experience = new Experience(
            source.Id,
            new Company(source.CompanyName),
            new Period(source.StartedAt, source.FinishedAt),
            MappingHelpers.ToRole(source.Role));

        experience.UpdateDescription(source.Description);
        experience.UpdateResponsibility(source.Responsibility);
        experience.UpdateType(source.Type);
        experience.ReplaceProjects(source.Projects?.Select(ProjectMapper.ToDomain).ToList());

        return experience;
    }

    public static ExperienceEntity ToEntity(Experience source) =>
        new()
        {
            Id = source.Id,
            CompanyName = source.Company.Name,
            StartedAt = source.Duration.StartedAt,
            FinishedAt = source.Duration.FinishedAt,
            Description = source.Description,
            Role = MappingHelpers.ToStoredRole(source.Role),
            Responsibility = source.Responsibility,
            Type = source.Type
        };
}
