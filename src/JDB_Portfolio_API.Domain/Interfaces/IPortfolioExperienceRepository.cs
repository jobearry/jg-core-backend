using JDB_Portfolio_API.Domain.Entities;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IPortfolioExperienceRepository : IRepository<Experience>
{
    Task<List<Experience>> GetAllExperienceWithProjectsAsync();
}
