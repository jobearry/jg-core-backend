using Portfolio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Domain.Interfaces;

public interface IPortfolioExperienceRepository : IRepository<Experience>
{
    Task<List<Experience>> GetAllExperienceWithProjectsAsync(CancellationToken cancellationToken);
}
