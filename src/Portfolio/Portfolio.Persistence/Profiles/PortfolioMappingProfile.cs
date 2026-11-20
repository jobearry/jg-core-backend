using AutoMapper;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Persistence.Entities;
namespace Portfolio.Persistence.Profiles;

public class PortfolioMappingProfile : Profile
{
    /// <summary>
    /// A blank role is not an error: it means the role is deliberately undeclared.
    /// <see cref="Role"/> rightly refuses to hold an empty string, so the mapping
    /// represents "undeclared" as the absence of a role. Without this, one blank
    /// row throws inside AutoMapper and takes down the whole collection endpoint.
    /// </summary>
    private static Role? ToRole(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new Role(value);

    /// <summary>
    /// The reverse of <see cref="ToRole"/>. The columns are still <c>NOT NULL</c>,
    /// so an undeclared role is stored as the empty string rather than <c>null</c>.
    /// </summary>
    private static string ToStoredRole(Role? role) => role?.Value ?? string.Empty;

    public PortfolioMappingProfile()
    {
        // TechStackEntity → TechStack
        //
        // Plan 3.3 — ConstructUsing plus ForAllMembers(Ignore) is deliberate, not
        // belt-and-braces: AutoMapper writes private setters through reflection, so
        // `private set` alone would leave the constructor's validation bypassable.
        // Ignoring every member is what makes the constructor the only way in.
        CreateMap<TechStackEntity, TechStack>()
            .ConstructUsing(src => new TechStack(
                src.Id,
                src.StackName,
                src.ImgSrc,
                src.TechStackCategory == null ? null : src.TechStackCategory.Category))
            .ForAllMembers(opt => opt.Ignore());

        // TechStack → TechStackEntity
        // The category is stored as an id and is resolved by a lookup when a write
        // path exists. It is never converted from the category name.
        CreateMap<TechStack, TechStackEntity>()
            .ForMember(dest => dest.StackName, opt => opt.MapFrom(src => src.StackName))
            .ForMember(dest => dest.ImgSrc, opt => opt.MapFrom(src => src.ImgSrc))
            // Plan 3.3 removed CreatedAt from the domain: it is audit bookkeeping, and
            // the column still has its own default. Ignored rather than mapped.
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TechStackCategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.TechStackCategory, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectTechStacks, opt => opt.Ignore());

        // TechStackCategoryEntity → TechStackCategory
        CreateMap<TechStackCategoryEntity, TechStackCategory>()
            .ConstructUsing(src => new TechStackCategory(src.Id, src.Category))
            .ForAllMembers(opt => opt.Ignore());

        // TechStackCategory → TechStackCategoryEntity
        CreateMap<TechStackCategory, TechStackCategoryEntity>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            // Same reason as TechStack above — CreatedAt is no longer on the domain type.
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TechStacks, opt => opt.Ignore());

        // ProjectEntity → Project
        CreateMap<ProjectEntity, Project>()
            .ConstructUsing(src => new Project(
                src.Id,
                src.ExperienceId,
                src.ProjectName,
                src.Description,
                src.Contribution))
            .ForMember(dest => dest.TechStacks, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.ProjectTechStacks != null && src.ProjectTechStacks.Any())
                {
                    dest.ReplaceTechStacks(src.ProjectTechStacks.Select(pts => new TechStackRef(
                        pts.TechStack.Id,
                        pts.TechStack.StackName,
                        pts.TechStack.ImgSrc,
                        pts.TechStack.TechStackCategory == null
                            ? null
                            : pts.TechStack.TechStackCategory.Category)));
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
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company.Name))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.Duration.StartedAt))
            .ForMember(dest => dest.FinishedAt, opt => opt.MapFrom(src => src.Duration.FinishedAt))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => ToStoredRole(src.Role)));

        // ExperienceEntity → Experience
        CreateMap<ExperienceEntity, Experience>()
            .ConstructUsing(src => new Experience(
                src.Id,
                // Required by the schema (NOT NULL + a non-blank CHECK, plan 3.1a), so
                // there is no null path to decide about here.
                new Company(src.CompanyName),
                new Period(src.StartedAt, src.FinishedAt),
                ToRole(src.Role)
            ))
            .ForMember(dest => dest.Duration, opt => opt.Ignore())
            .ForMember(dest => dest.Projects, opt => opt.Ignore())
            // Set by ConstructUsing via ToRole. Without the Ignore, AutoMapper maps
            // string -> Role? by name afterwards and throws on a blank value.
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                // Map the Projects collection with their TechStacks. ReplaceProjects
                // copies the argument, so the aggregate keeps control of its collection.
                if (src.Projects != null && src.Projects.Any())
                {
                    dest.ReplaceProjects(src.Projects.Select(p => new Project(
                        p.Id,
                        p.ExperienceId,
                        p.ProjectName,
                        p.Description,
                        p.Contribution,
                        p.ProjectTechStacks != null && p.ProjectTechStacks.Any()
                            ? p.ProjectTechStacks.Select(pts => new TechStackRef(
                                pts.TechStack.Id,
                                pts.TechStack.StackName,
                                pts.TechStack.ImgSrc,
                                pts.TechStack.TechStackCategory == null
                                    ? null
                                    : pts.TechStack.TechStackCategory.Category))
                            : null)));
                }
            });

        // RecommendationEntity → Recommendation
        // The constructor owns every member, so nothing is mapped afterwards.
        CreateMap<RecommendationEntity, Recommendation>()
            .ConstructUsing(src => new Recommendation(
                src.Id,
                src.PersonName,
                src.Description,
                ToRole(src.CareerPosition),
                src.PostedAt
            ))
            .ForMember(dest => dest.PersonName, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.Ignore())
            .ForMember(dest => dest.CareerPosition, opt => opt.Ignore())
            .ForMember(dest => dest.PostedAt, opt => opt.Ignore());

        // Recommendation → RecommendationEntity
        CreateMap<Recommendation, RecommendationEntity>()
            .ForMember(dest => dest.PersonName, opt => opt.MapFrom(src => src.PersonName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.CareerPosition, opt => opt.MapFrom(src => ToStoredRole(src.CareerPosition)))
            .ForMember(dest => dest.PostedAt, opt => opt.MapFrom(src => src.PostedAt));
    }
}
