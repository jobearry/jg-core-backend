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
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src =>
                src.TechStackCategory == null ? null : src.TechStackCategory.Category))
            .ConstructUsing(src => new TechStack
            {
                Id = src.Id,
                StackName = src.StackName,
                ImgSrc = src.ImgSrc,
                CreatedAt = src.CreatedAt
            });

        // TechStack → TechStackEntity
        // The category is stored as an id and is resolved by a lookup when a write
        // path exists. It is never converted from the category name.
        CreateMap<TechStack, TechStackEntity>()
            .ForMember(dest => dest.StackName, opt => opt.MapFrom(src => src.StackName))
            .ForMember(dest => dest.ImgSrc, opt => opt.MapFrom(src => src.ImgSrc))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.TechStackCategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.TechStackCategory, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectTechStacks, opt => opt.Ignore());

        // TechStackCategoryEntity → TechStackCategory
        CreateMap<TechStackCategoryEntity, TechStackCategory>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // TechStackCategory → TechStackCategoryEntity
        CreateMap<TechStackCategory, TechStackCategoryEntity>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.TechStacks, opt => opt.Ignore());

        // ProjectEntity → Project
        CreateMap<ProjectEntity, Project>()
            .ForMember(dest => dest.ExperienceId, opt => opt.MapFrom(src => src.ExperienceId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Contribution, opt => opt.MapFrom(src => src.Contribution))
            .ForMember(dest => dest.TechStacks, opt => opt.Ignore())
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
            .ForMember(dest => dest.Contribution, opt => opt.MapFrom(src => src.Contribution))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Experience, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectTechStacks, opt => opt.Ignore());

        // Experience → ExperienceEntity
        CreateMap<Experience, ExperienceEntity>()
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.Duration.StartedAt))
            .ForMember(dest => dest.FinishedAt, opt => opt.MapFrom(src => src.Duration.FinishedAt))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        // ExperienceEntity → Experience
        CreateMap<ExperienceEntity, Experience>()
            .ConstructUsing(src => new Experience(
                src.Id,
                src.CompanyName != null ? new Company(src.CompanyName) : null,
                new Period(src.StartedAt, src.FinishedAt),
                new Role(src.Role)
            ))
            .ForMember(dest => dest.Duration, opt => opt.Ignore())
            .ForMember(dest => dest.Projects, opt => opt.Ignore())
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

        // RecommendationEntity → Recommendation
        CreateMap<RecommendationEntity, Recommendation>()
            .ConstructUsing(src => new Recommendation(
                src.Id,
                src.PersonName,
                src.Description,
                new Role(src.CareerPosition),
                src.PostedAt
            ));

        // Recommendation → RecommendationEntity
        CreateMap<Recommendation, RecommendationEntity>()
            .ForMember(dest => dest.PersonName, opt => opt.MapFrom(src => src.PersonName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.CareerPosition, opt => opt.MapFrom(src => src.CareerPosition))
            .ForMember(dest => dest.PostedAt, opt => opt.MapFrom(src => src.PostedAt));
    }
}
