using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioSkillService : PortfolioServiceBase<TechStack, IPortfolioTechStackRepository>
{
    public PortfolioSkillService(IPortfolioTechStackRepository repository, ILogger<PortfolioSkillService> logger)
        : base(repository, logger) { }

    public async Task<Result<List<TechStackDto>>> GetAllTechStacksAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var techStacks = await Repository.GetAllTechStacksAsync(ct);

            return techStacks
                .OrderBy(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio tech stack content", cancellationToken);
    }
}
