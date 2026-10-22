using JDB_Portfolio_API.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IPortfolioExperienceRepository : IRepository<Experience>
{
    Task<List<Experience>> GetAllExperienceWithProjectsAsync(CancellationToken cancellationToken);
}
