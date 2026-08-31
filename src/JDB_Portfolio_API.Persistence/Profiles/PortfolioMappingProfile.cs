using AutoMapper;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;
using JDB_Portfolio_API.Persistence.Entities;
namespace JDB_Portfolio_API.Persistence.Profiles;

public class PortfolioMappingProfile : Profile
{
    public PortfolioMappingProfile()
    {
        // TechStackEntity → TechStack
        CreateMap<TechStackEntity, TechStack>()
            .ForMember(dest => dest.StackName, opt => opt.MapFrom(src => src.StackName))
            .ForMember(dest => dest.ImgSrc, opt => opt.MapFrom(src => src.ImgSrc))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ConstructUsing(dest => new TechStack
            {
                Id = dest.Id,
                StackName = dest.StackName,
                ImgSrc = dest.ImgSrc,
                CreatedAt = dest.CreatedAt
            })
            .AfterMap((src, dest) =>
            {
                // Map the TechStackCategoryId to the Category property
                if (src.TechStackCategory != null)
                {
                    dest.Category = src.TechStackCategory.Category;
                }
            });

        // TechStack → TechStackEntity
        CreateMap<TechStack, TechStackEntity>()
            .ForMember(dest => dest.StackName, opt => opt.MapFrom(src => src.StackName))
            .ForMember(dest => dest.ImgSrc, opt => opt.MapFrom(src => src.ImgSrc))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.TechStackCategoryId, opt => opt.MapFrom(src => src.Category));

        // TechStackCategoryEntity → TechStackCategory
        CreateMap<TechStackCategoryEntity, TechStackCategory>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // TechStackCategory → TechStackCategoryEntity
        CreateMap<TechStackCategory, TechStackCategoryEntity>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // ProjectEntity → Project
        CreateMap<ProjectEntity, Project>()
            .ForMember(dest => dest.ExperienceId, opt => opt.MapFrom(src => src.ExperienceId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Contribution, opt => opt.MapFrom(src => src.Contribution))
            .ConstructUsing(src => new Project(
                src.Id,
                src.ProjectName
            ))
            .AfterMap((src, dest) =>
            {
                if(src.ProjectTechStacks != null && src.ProjectTechStacks.Any())
                {
                    dest.TechStacks = src.ProjectTechStacks.Select(pts => new TechStack
                    {
                        Id = pts.TechStack.Id,
                        StackName = pts.TechStack.StackName,
                        ImgSrc = pts.TechStack.ImgSrc,
                        CreatedAt = pts.TechStack.CreatedAt,
                        Category = pts.TechStack.TechStackCategory.Category
                    }).ToList();
                }
            });

        // Project → ProjectEntity
        CreateMap<Project, ProjectEntity>()
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.ProjectName))
            .ForMember(dest => dest.ExperienceId, opt => opt.MapFrom(src => src.ExperienceId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Contribution, opt => opt.MapFrom(src => src.Contribution));

        // Experience → ExperienceEntity
        CreateMap<Experience, ExperienceEntity>()
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.Duration.StartedAt))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        // ExperienceEntity → Experience
        CreateMap<ExperienceEntity, Experience>()
            .ConstructUsing(src => new Experience(
                src.Id,
                src.CompanyName != null ? new Company(src.CompanyName) : null,
                new Period(src.StartedAt, null),
                new Role(src.Role)
            ))
            .AfterMap((src, dest) =>
            {
                // Map the Projects collection with their TechStacks
                if (src.Projects != null && src.Projects.Any())
                {
                    dest.Projects = src.Projects.Select(p => new Project(p.Id, p.ProjectName)
                    {
                        ExperienceId = p.ExperienceId,
                        Description = p.Description,
                        Contribution = p.Contribution,
                        TechStacks = p.ProjectTechStacks != null && p.ProjectTechStacks.Any()
                            ? p.ProjectTechStacks.Select(pts => new TechStack
                            {
                                Id = pts.TechStack.Id,
                                StackName = pts.TechStack.StackName,
                                ImgSrc = pts.TechStack.ImgSrc,
                                CreatedAt = pts.TechStack.CreatedAt,
                                Category = pts.TechStack.TechStackCategory.Category
                            }).ToList()
                            : new List<TechStack>()
                    }).ToList();
                }
            });
    }
}
