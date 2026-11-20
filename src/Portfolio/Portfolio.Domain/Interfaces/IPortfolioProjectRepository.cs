using Portfolio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Domain.Interfaces;

public interface IPortfolioProjectRepository : IRepository<Project>
{
    Task<List<Project>> GetAllProjectsWithTechStacksAsync(CancellationToken cancellationToken);
}
