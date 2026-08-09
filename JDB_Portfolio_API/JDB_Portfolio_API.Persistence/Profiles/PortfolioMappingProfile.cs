using AutoMapper;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;
using JDB_Portfolio_API.Persistence.Entities;
namespace JDB_Portfolio_API.Persistence.Profiles;

public class PortfolioMappingProfile : Profile
{
    public PortfolioMappingProfile()
    {
        // ProjectEntity → Project
        CreateMap<ProjectEntity, Project>()
            .ConstructUsing(src => new Project(src.Id, src.ProjectName))
            .ForMember(dest => dest.ExperienceId, opt => opt.MapFrom(src => src.ExperienceId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Contribution, opt => opt.MapFrom(src => src.Contribution));

        // Project → ProjectEntity
        CreateMap<Project, ProjectEntity>()
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.ProjectName))
            .ForMember(dest => dest.ExperienceId, opt => opt.MapFrom(src => src.ExperienceId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Contribution, opt => opt.MapFrom(src => src.Contribution));

        // Domain → Entity
        CreateMap<Experience, ExperienceEntity>()
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.Duration.StartedAt))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        // Entity → Domain
        CreateMap<ExperienceEntity, Experience>()
            .ConstructUsing(src => new Experience(
                src.Id,
                src.CompanyName != null ? new Company(src.CompanyName) : null,
                new Period(src.StartedAt, null),
                new Role(src.Role)
            ))
            .AfterMap((src, dest) =>
            {
                // Map the Projects collection from ExperienceEntity to Experience
                if (src.Projects != null && src.Projects.Any())
                {
                    dest.Projects = src.Projects.Select(p => new Project(p.Id, p.ProjectName)
                    {
                        ExperienceId = p.ExperienceId,
                        Description = p.Description,
                        Contribution = p.Contribution
                    }).ToList();
                }
            });
    }
}
