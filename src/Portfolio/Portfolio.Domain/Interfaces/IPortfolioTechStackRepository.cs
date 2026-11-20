using Portfolio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Domain.Interfaces;

public interface IPortfolioTechStackRepository : IRepository<TechStack>
{
    Task<List<TechStack>> GetAllTechStacksAsync(CancellationToken cancellationToken);
}
