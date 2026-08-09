using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IPortfolioExperienceRepository : IRepository<Experience>
{
    //Task<Experience?> GetExperienceAsync(ExperienceId id);
    Task<List<Experience?>> GetAllExperienceWithProjects();
}
