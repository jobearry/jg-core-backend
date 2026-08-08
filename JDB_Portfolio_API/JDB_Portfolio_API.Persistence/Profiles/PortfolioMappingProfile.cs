using AutoMapper;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;
using JDB_Portfolio_API.Persistence.Entities;
namespace JDB_Portfolio_API.Persistence.Profiles;

public class PortfolioMappingProfile : Profile
{
    public PortfolioMappingProfile()
    {
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
            ));
    }
}
